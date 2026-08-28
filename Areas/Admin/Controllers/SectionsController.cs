using System.Security.Claims;
using System.Text.Json;
using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LandingCms.Services;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator,Editor")]
public class SectionsController(ApplicationDbContext db, IMediaStorageService mediaStorage, IContentHtmlSanitizer htmlSanitizer, ISectionSchemaService sectionSchemas) : Controller
{
    public async Task<IActionResult> Index()
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var slots = await db.TemplateSections.AsNoTracking().Include(x => x.Template).Include(x => x.SectionDefinition)
            .Where(x => x.TemplateId == setting.ActiveTemplateId).OrderBy(x => x.SortOrder).ToListAsync();
        var keys = slots.Select(x => x.SectionKey).ToArray();
        var contents = await db.SectionContents.AsNoTracking().Where(x => keys.Contains(x.SectionKey)).ToDictionaryAsync(x => x.SectionKey);
        ViewBag.TemplateName = slots.FirstOrDefault()?.Template.Name ?? "";
        return View(slots.Select(x => new SectionListItemViewModel(x, contents.GetValueOrDefault(x.SectionKey))).ToList());
    }

    public async Task<IActionResult> Edit(int id)
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var slot = await db.TemplateSections.AsNoTracking().Include(x => x.SectionDefinition)
            .FirstOrDefaultAsync(x => x.Id == id && x.TemplateId == setting.ActiveTemplateId);
        if (slot is null) return NotFound();
        var content = await db.SectionContents.AsNoTracking().FirstOrDefaultAsync(x => x.SectionKey == slot.SectionKey);
        var payload = content is null ? new SectionContentPayload() : JsonSerializer.Deserialize<SectionContentPayload>(content.ContentJson) ?? new();
        var contentField = sectionSchemas.GetField(slot.SectionDefinition.SchemaJson, "content");
        if (contentField.Editor == "html")
            payload.Content = htmlSanitizer.Sanitize(payload.Content, contentField.HtmlPolicy);
        var model = new SectionContentEditViewModel
        {
            TemplateSectionId = slot.Id, ContentId = content?.Id, SectionKey = slot.SectionKey,
            SectionType = slot.SectionDefinition.SectionType, DisplayName = slot.DisplayName,
            ContentEditor = contentField.Editor, ContentHtmlPolicy = contentField.HtmlPolicy,
            AllowedHtmlTags = htmlSanitizer.GetAllowedTags(contentField.HtmlPolicy),
            Eyebrow = payload.Eyebrow, Title = payload.Title, Subtitle = payload.Subtitle, Content = payload.Content,
            ImageUrl = payload.ImageUrl, PrimaryButtonText = payload.PrimaryButtonText, PrimaryButtonUrl = payload.PrimaryButtonUrl,
            SecondaryButtonText = payload.SecondaryButtonText, SecondaryButtonUrl = payload.SecondaryButtonUrl,
            IsEnabled = slot.IsEnabled
        };
        model.Backgrounds = await LoadBackgroundsAsync(slot.SectionKey);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SectionContentEditViewModel model)
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var slot = await db.TemplateSections.Include(x => x.SectionDefinition)
            .FirstOrDefaultAsync(x => x.Id == model.TemplateSectionId && x.TemplateId == setting.ActiveTemplateId);
        if (slot is null) return NotFound();
        var contentField = sectionSchemas.GetField(slot.SectionDefinition.SchemaJson, "content");
        model.ContentEditor = contentField.Editor; model.ContentHtmlPolicy = contentField.HtmlPolicy;
        model.AllowedHtmlTags = htmlSanitizer.GetAllowedTags(contentField.HtmlPolicy);
        if (contentField.Editor == "html")
            model.Content = htmlSanitizer.Sanitize(model.Content, contentField.HtmlPolicy);
        var canManageVisibility = User.IsInRole(DbInitializer.SuperAdministrator);
        if (!canManageVisibility) model.IsEnabled = slot.IsEnabled;
        if (canManageVisibility && slot.IsRequired && !model.IsEnabled) ModelState.AddModelError(nameof(model.IsEnabled), "Section bắt buộc không thể bị ẩn.");
        if (!ModelState.IsValid) { await PrepareForViewAsync(model, slot); return View("Edit", model); }
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (model.ImageFile is { Length: > 0 })
                model.ImageUrl = (await mediaStorage.SaveImageAsync(model.ImageFile, userId, ImageUploadProfile.SectionImage, HttpContext.RequestAborted)).RelativeUrl;
            if (slot.SectionDefinition.SectionType == "Hero" && model.BackgroundFiles.Count > 0)
            {
                var nextOrder = (await db.SectionMedia.Where(x => x.SectionKey == slot.SectionKey && x.Role == "Background").MaxAsync(x => (int?)x.SortOrder) ?? 0) + 10;
                foreach (var file in model.BackgroundFiles.Where(x => x.Length > 0))
                {
                    var asset = await mediaStorage.SaveImageAsync(file, userId, ImageUploadProfile.HeroBackground, HttpContext.RequestAborted);
                    db.SectionMedia.Add(new SectionMedia { SectionKey = slot.SectionKey, MediaAssetId = asset.Id, Role = "Background", SortOrder = nextOrder });
                    nextOrder += 10;
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message); await PrepareForViewAsync(model, slot); return View("Edit", model);
        }
        var content = await db.SectionContents.FirstOrDefaultAsync(x => x.SectionKey == slot.SectionKey);
        if (content is null) { content = new SectionContent { SectionKey = slot.SectionKey, SectionDefinitionId = slot.SectionDefinitionId }; db.SectionContents.Add(content); }
        content.ContentJson = DatabaseJson.Serialize(new SectionContentPayload
        {
            Eyebrow = model.Eyebrow, Title = model.Title, Subtitle = model.Subtitle, Content = model.Content,
            ImageUrl = model.ImageUrl, PrimaryButtonText = model.PrimaryButtonText, PrimaryButtonUrl = model.PrimaryButtonUrl,
            SecondaryButtonText = model.SecondaryButtonText, SecondaryButtonUrl = model.SecondaryButtonUrl
        });
        if (canManageVisibility) slot.IsEnabled = model.IsEnabled;
        content.UpdatedAtUtc = DateTime.UtcNow;
        content.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã lưu {slot.DisplayName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBackground(long id)
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var media = await db.SectionMedia.FirstOrDefaultAsync(x => x.Id == id && x.Role == "Background");
        if (media is null) return NotFound();
        var slot = await db.TemplateSections.AsNoTracking().FirstOrDefaultAsync(x => x.TemplateId == setting.ActiveTemplateId && x.SectionKey == media.SectionKey);
        if (slot is null) return NotFound();
        db.SectionMedia.Remove(media); await db.SaveChangesAsync();
        TempData["Message"] = "Đã gỡ ảnh khỏi Hero. File vẫn được giữ trong Media Library.";
        return RedirectToAction(nameof(Edit), new { id = slot.Id });
    }

    private async Task PrepareForViewAsync(SectionContentEditViewModel model, TemplateSection slot)
    {
        model.SectionKey = slot.SectionKey; model.SectionType = slot.SectionDefinition.SectionType; model.DisplayName = slot.DisplayName;
        var contentField = sectionSchemas.GetField(slot.SectionDefinition.SchemaJson, "content");
        model.ContentEditor = contentField.Editor; model.ContentHtmlPolicy = contentField.HtmlPolicy;
        model.AllowedHtmlTags = htmlSanitizer.GetAllowedTags(contentField.HtmlPolicy);
        model.Backgrounds = await LoadBackgroundsAsync(slot.SectionKey);
    }

    private async Task<IReadOnlyList<SectionMedia>> LoadBackgroundsAsync(string sectionKey) => await db.SectionMedia.AsNoTracking()
        .Include(x => x.MediaAsset).Where(x => x.SectionKey == sectionKey && x.Role == "Background")
        .OrderBy(x => x.SortOrder).ToListAsync();
}
