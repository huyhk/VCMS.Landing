using System.Security.Claims;
using System.Text.Json;
using LandingCms.Data;
using LandingCms.Models;
using LandingCms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = DbInitializer.SuperAdministrator)]
public sealed class AiContentController(
    ApplicationDbContext db,
    IAiContentService ai,
    IAiDraftStore drafts,
    ISectionSchemaService schemas,
    IContentHtmlSanitizer htmlSanitizer,
    ILogger<AiContentController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.IsAvailable = ai.IsAvailable;
        ViewBag.ModelName = ai.ModelName;
        ViewBag.TemplateName = await GetActiveTemplateNameAsync();
        return View(new AiLandingBrief());
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-content")]
    public async Task<IActionResult> Generate(AiLandingBrief brief)
    {
        ViewBag.IsAvailable = ai.IsAvailable;
        ViewBag.ModelName = ai.ModelName;
        ViewBag.TemplateName = await GetActiveTemplateNameAsync();
        if (!ai.IsAvailable)
        {
            ModelState.AddModelError("", "AI Content Studio chưa được bật hoặc thiếu API key.");
            return View("Index", brief);
        }
        if (!ModelState.IsValid) return View("Index", brief);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var templateSetting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
            var template = await db.PageTemplates.AsNoTracking().FirstAsync(x => x.Id == templateSetting.ActiveTemplateId);
            var language = await db.ContentLanguages.AsNoTracking().FirstAsync(x => x.IsDefault && x.IsEnabled);
            var slots = await db.TemplateSections.AsNoTracking().Include(x => x.SectionDefinition)
                .Where(x => x.TemplateId == template.Id)
                .OrderBy(x => x.SortOrder).ToListAsync();
            var request = new AiContentRequest
            {
                Brief = brief, LanguageCode = language.Code, LanguageName = language.Name,
                Sections = slots.Select(ToSpec).ToList()
            };
            var draft = await ai.GenerateLandingAsync(request, HttpContext.RequestAborted);
            draft.UserId = userId;
            draft.TemplateId = template.Id;
            draft.TemplateName = template.Name;
            drafts.Set(draft);
            return RedirectToAction(nameof(Preview), new { token = draft.Token });
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "AI draft generation failed for {User}", User.Identity?.Name);
            ModelState.AddModelError("", ex.Message);
            return View("Index", brief);
        }
    }

    [HttpGet]
    public IActionResult Preview(string token)
    {
        var draft = GetDraft(token);
        return draft is null ? DraftExpired() : View(draft);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(string token, string[] sectionKeys)
    {
        var draft = GetDraft(token);
        if (draft is null) return DraftExpired();
        var selected = sectionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selected.Count == 0)
        {
            TempData["Error"] = "Hãy chọn ít nhất một section để áp dụng.";
            return RedirectToAction(nameof(Preview), new { token });
        }

        var activeTemplateId = (await db.SiteTemplateSettings.AsNoTracking().FirstAsync()).ActiveTemplateId;
        if (activeTemplateId != draft.TemplateId)
        {
            TempData["Error"] = "Template đang dùng đã thay đổi. Hãy tạo lại bản nháp AI.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var slots = await db.TemplateSections.Include(x => x.SectionDefinition)
                .Where(x => x.TemplateId == activeTemplateId && selected.Contains(x.SectionKey)).ToListAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var applied = 0;
            await using var transaction = await db.Database.BeginTransactionAsync();
            foreach (var slot in slots)
            {
                var proposed = draft.Sections.FirstOrDefault(x => x.SectionKey.Equals(slot.SectionKey, StringComparison.OrdinalIgnoreCase));
                if (proposed is null || (proposed.Content.Count == 0 && proposed.Items.Count == 0)) continue;
                var schema = schemas.GetSchema(slot.SectionDefinition.SchemaJson) ?? new SectionSchemaDocument();
                await ApplyContentAsync(slot, schema, proposed, userId);
                await ApplyItemsAsync(slot, schema, proposed, userId);
                applied++;
            }
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            drafts.Remove(token, draft.UserId);
            TempData["Message"] = $"Đã áp dụng nội dung AI cho {applied} section. Nội dung chưa được AI tạo vẫn được giữ nguyên.";
            return RedirectToAction("Index", "Sections", new { area = "Admin" });
        }
        catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException or JsonException)
        {
            logger.LogError(ex, "Could not apply AI content draft {Token}", token);
            TempData["Error"] = "Không thể áp dụng bản nháp. Nội dung hiện tại chưa bị thay đổi.";
            return RedirectToAction(nameof(Preview), new { token });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel(string token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        drafts.Remove(token, userId);
        TempData["Message"] = "Đã hủy bản nháp AI.";
        return RedirectToAction(nameof(Index));
    }

    private AiTemplateSectionSpec ToSpec(TemplateSection slot)
    {
        var schema = schemas.GetSchema(slot.SectionDefinition.SchemaJson) ?? new SectionSchemaDocument();
        return new AiTemplateSectionSpec
        {
            SectionKey = slot.SectionKey, DisplayName = slot.DisplayName,
            SectionType = slot.SectionDefinition.SectionType,
            Fields = schema.Fields, ItemFields = schema.Items?.Fields ?? new Dictionary<string, SectionFieldSchema>()
        };
    }

    private async Task ApplyContentAsync(TemplateSection slot, SectionSchemaDocument schema, AiSectionDraft proposed, string? userId)
    {
        var entity = await db.SectionContents.FirstOrDefaultAsync(x => x.SectionKey == slot.SectionKey);
        if (entity is null)
        {
            entity = new SectionContent { SectionKey = slot.SectionKey, SectionDefinitionId = slot.SectionDefinitionId };
            db.SectionContents.Add(entity);
        }
        var values = Deserialize(entity.ContentJson);
        foreach (var field in proposed.Content)
        {
            var definition = FindField(schema.Fields, field.Key);
            if (definition is null || definition.Editor == "image") continue;
            var value = CleanValue(field.Key, field.Value, definition);
            if (value is null) continue;
            values[Pascal(field.Key)] = value;
        }
        entity.ContentJson = JsonSerializer.Serialize(values);
        entity.IsPublished = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.UpdatedById = userId;
        db.ContentRevisions.Add(new ContentRevision
        {
            EntityType = "SectionContent", EntityKey = slot.SectionKey, Action = "AI Applied",
            DisplayName = slot.DisplayName,
            SnapshotJson = JsonSerializer.Serialize(new { entity.ContentJson, slot.IsEnabled }),
            CreatedById = userId, CreatedByName = User.Identity?.Name
        });
    }

    private async Task ApplyItemsAsync(TemplateSection slot, SectionSchemaDocument schema, AiSectionDraft proposed, string? userId)
    {
        if (schema.Items is null || proposed.Items.Count == 0) return;
        var existing = await db.SectionItems.Where(x => x.SectionKey == slot.SectionKey)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        for (var index = 0; index < proposed.Items.Count; index++)
        {
            var entity = index < existing.Count ? existing[index] : new SectionItem
            {
                SectionKey = slot.SectionKey, SortOrder = (index + 1) * 10, IsEnabled = true
            };
            if (entity.Id == 0 && schema.Items.Fields.Values.Any(x => x.Required && x.Editor == "image"))
                continue; // AI text mode cannot satisfy a required media field.
            var values = Deserialize(entity.ContentJson);
            foreach (var field in proposed.Items[index])
            {
                var definition = FindField(schema.Items.Fields, field.Key);
                if (definition is null || definition.Editor == "image") continue;
                var value = CleanValue(field.Key, field.Value, definition);
                if (value is not null) values[field.Key] = value;
            }
            if (!HasRequiredValues(values, schema.Items.Fields)) continue;
            entity.ContentJson = JsonSerializer.Serialize(values);
            entity.UpdatedAtUtc = DateTime.UtcNow;
            entity.UpdatedById = userId;
            if (entity.Id == 0)
            {
                db.SectionItems.Add(entity);
                await db.SaveChangesAsync();
            }
            db.ContentRevisions.Add(new ContentRevision
            {
                EntityType = "SectionItem", EntityKey = $"{slot.SectionKey}:{entity.Id}", Action = "AI Applied",
                DisplayName = slot.DisplayName,
                SnapshotJson = JsonSerializer.Serialize(new { entity.ContentJson, entity.MediaAssetId, entity.IsEnabled }),
                CreatedById = userId, CreatedByName = User.Identity?.Name
            });
        }
    }

    private string? CleanValue(string key, string? value, SectionFieldSchema field)
    {
        if (value is null) return null;
        value = value.Trim();
        if (value.Length == 0) return null;
        if (field.Editor == "html") value = htmlSanitizer.Sanitize(value, field.HtmlPolicy);
        if ((field.Editor == "url" || key.EndsWith("Url", StringComparison.OrdinalIgnoreCase)) && value.Length > 0)
        {
            var normalized = PublicLinkUrl.Normalize(value);
            if (normalized is null) return null;
            value = normalized;
        }
        if (field.Editor == "media-url" && value.Length > 0 && !MediaEmbedUrl.TryResolve(value, out _)) return null;
        if (field.Editor == "select" && value.Length > 0 && !field.Options.Any(x => x.Value == value)) return null;
        return value;
    }

    private static SectionFieldSchema? FindField(IReadOnlyDictionary<string, SectionFieldSchema> fields, string key) =>
        fields.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;

    private static bool HasRequiredValues(IReadOnlyDictionary<string, string?> values, IReadOnlyDictionary<string, SectionFieldSchema> fields) =>
        fields.Where(x => x.Value.Required && x.Value.Editor != "image")
            .All(x => values.TryGetValue(x.Key, out var value) && !string.IsNullOrWhiteSpace(value));

    private static Dictionary<string, string?> Deserialize(string? json)
    {
        try { return new Dictionary<string, string?>(JsonSerializer.Deserialize<Dictionary<string, string?>>(json ?? "{}") ?? [], StringComparer.OrdinalIgnoreCase); }
        catch (JsonException) { return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase); }
    }

    private static string Pascal(string value) => value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
    private AiLandingDraft? GetDraft(string token) => drafts.Get(token, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IActionResult DraftExpired() { TempData["Error"] = "Bản nháp AI không còn tồn tại hoặc đã hết hạn."; return RedirectToAction(nameof(Index)); }
    private async Task<string> GetActiveTemplateNameAsync()
    {
        var id = (await db.SiteTemplateSettings.AsNoTracking().FirstAsync()).ActiveTemplateId;
        return await db.PageTemplates.AsNoTracking().Where(x => x.Id == id).Select(x => x.Name).FirstAsync();
    }
}
