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
            IsEnabled = slot.IsEnabled,
            HasItems = sectionSchemas.GetItems(slot.SectionDefinition.SchemaJson) is not null
        };
        model.Backgrounds = await LoadBackgroundsAsync(slot.SectionKey);
        model.GalleryImages = await LoadMediaAsync(slot.SectionKey, "Gallery");
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
            var mainImage = await db.SectionMedia.Include(x => x.MediaAsset)
                .FirstOrDefaultAsync(x => x.SectionKey == slot.SectionKey && x.Role == "MainImage");
            if (model.ImageFile is { Length: > 0 })
            {
                var asset = await mediaStorage.SaveImageAsync(model.ImageFile, userId, ImageUploadProfile.SectionImage, HttpContext.RequestAborted);
                model.ImageUrl = asset.RelativeUrl;
                if (mainImage is null)
                {
                    db.SectionMedia.Add(new SectionMedia
                    {
                        SectionKey = slot.SectionKey, MediaAssetId = asset.Id, Role = "MainImage", SortOrder = 0
                    });
                }
                else
                {
                    mainImage.MediaAssetId = asset.Id;
                }
            }
            else if (mainImage is not null && !string.Equals(model.ImageUrl, mainImage.MediaAsset.RelativeUrl, StringComparison.OrdinalIgnoreCase))
            {
                // An explicitly entered external URL (or an empty value) takes precedence.
                db.SectionMedia.Remove(mainImage);
            }
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
            if (slot.SectionDefinition.SectionType == "Gallery" && model.GalleryFiles.Count > 0)
            {
                var nextOrder = (await db.SectionMedia.Where(x => x.SectionKey == slot.SectionKey && x.Role == "Gallery").MaxAsync(x => (int?)x.SortOrder) ?? 0) + 10;
                foreach (var file in model.GalleryFiles.Where(x => x.Length > 0))
                {
                    var asset = await mediaStorage.SaveImageAsync(file, userId, ImageUploadProfile.SectionImage, HttpContext.RequestAborted);
                    db.SectionMedia.Add(new SectionMedia { SectionKey = slot.SectionKey, MediaAssetId = asset.Id, Role = "Gallery", SortOrder = nextOrder });
                    nextOrder += 10;
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message); await PrepareForViewAsync(model, slot); return View("Edit", model);
        }
        var content = await db.SectionContents.FirstOrDefaultAsync(x => x.SectionKey == slot.SectionKey);
        var contentIsNew = content is null;
        if (content is null) { content = new SectionContent { SectionKey = slot.SectionKey, SectionDefinitionId = slot.SectionDefinitionId }; db.SectionContents.Add(content); }
        content.ContentJson = JsonSerializer.Serialize(new SectionContentPayload
        {
            Eyebrow = model.Eyebrow, Title = model.Title, Subtitle = model.Subtitle, Content = model.Content,
            ImageUrl = model.ImageUrl, PrimaryButtonText = model.PrimaryButtonText, PrimaryButtonUrl = model.PrimaryButtonUrl,
            SecondaryButtonText = model.SecondaryButtonText, SecondaryButtonUrl = model.SecondaryButtonUrl
        });
        if (canManageVisibility) slot.IsEnabled = model.IsEnabled;
        content.UpdatedAtUtc = DateTime.UtcNow;
        content.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        AddSectionContentRevision(content, slot, contentIsNew ? "Created" : "Saved");
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGalleryImage(long id)
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var media = await db.SectionMedia.FirstOrDefaultAsync(x => x.Id == id && x.Role == "Gallery");
        if (media is null) return NotFound();
        var slot = await db.TemplateSections.AsNoTracking().FirstOrDefaultAsync(x => x.TemplateId == setting.ActiveTemplateId && x.SectionKey == media.SectionKey);
        if (slot is null) return NotFound();
        db.SectionMedia.Remove(media); await db.SaveChangesAsync();
        TempData["Message"] = "Đã gỡ ảnh khỏi thư viện. File vẫn được giữ trong Media Library.";
        return RedirectToAction(nameof(Edit), new { id = slot.Id });
    }

    public async Task<IActionResult> Items(int id)
    {
        var slot = await FindActiveSlotAsync(id);
        if (slot is null) return NotFound();
        if (sectionSchemas.GetItems(slot.SectionDefinition.SchemaJson) is null) return NotFound();
        var items = await db.SectionItems.AsNoTracking().Include(x => x.MediaAsset)
            .Where(x => x.SectionKey == slot.SectionKey).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        return View(new SectionItemListViewModel(slot, items));
    }

    [HttpGet]
    public async Task<IActionResult> EditItem(int sectionId, long? id)
    {
        var slot = await FindActiveSlotAsync(sectionId);
        if (slot is null) return NotFound();
        var itemSchema = sectionSchemas.GetItems(slot.SectionDefinition.SchemaJson);
        if (itemSchema is null) return NotFound();
        SectionItem? item = null;
        if (id.HasValue)
        {
            item = await db.SectionItems.AsNoTracking().Include(x => x.MediaAsset)
                .FirstOrDefaultAsync(x => x.Id == id && x.SectionKey == slot.SectionKey);
            if (item is null) return NotFound();
        }
        return View(new SectionItemEditViewModel
        {
            Id = item?.Id, TemplateSectionId = slot.Id, SectionKey = slot.SectionKey,
            DisplayName = slot.DisplayName, Fields = itemSchema.Fields,
            AllowedHtmlTags = GetAllowedItemTags(itemSchema),
            Values = DeserializeItemValues(item?.ContentJson), MediaAsset = item?.MediaAsset,
            IsEnabled = item?.IsEnabled ?? true
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItem(SectionItemEditViewModel model)
    {
        var slot = await FindActiveSlotAsync(model.TemplateSectionId);
        if (slot is null) return NotFound();
        var itemSchema = sectionSchemas.GetItems(slot.SectionDefinition.SchemaJson);
        if (itemSchema is null) return NotFound();
        model.SectionKey = slot.SectionKey; model.DisplayName = slot.DisplayName; model.Fields = itemSchema.Fields;
        model.AllowedHtmlTags = GetAllowedItemTags(itemSchema);
        model.Values ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in itemSchema.Fields)
        {
            model.Values.TryGetValue(field.Key, out var value);
            value = value?.Trim();
            if (field.Value.Required && string.IsNullOrWhiteSpace(value) && field.Value.Editor != "image")
                ModelState.AddModelError($"Values[{field.Key}]", $"{field.Value.Label} là bắt buộc.");
            if (field.Value.Editor == "html") value = htmlSanitizer.Sanitize(value, field.Value.HtmlPolicy);
            if (field.Value.Editor == "media-url" && !string.IsNullOrWhiteSpace(value) && !MediaEmbedUrl.TryResolve(value, out _))
                ModelState.AddModelError($"Values[{field.Key}]", "Chỉ hỗ trợ URL video YouTube hoặc Vimeo hợp lệ.");
            if (field.Value.Editor == "url" && !string.IsNullOrWhiteSpace(value) && PublicLinkUrl.Normalize(value) is null)
                ModelState.AddModelError($"Values[{field.Key}]", "Liên kết không hợp lệ. Chỉ hỗ trợ HTTP(S), email, số điện thoại hoặc liên kết nội bộ.");
            if (field.Value.Editor == "select" && !string.IsNullOrWhiteSpace(value) && !field.Value.Options.Any(x => x.Value == value))
                ModelState.AddModelError($"Values[{field.Key}]", "Giá trị lựa chọn không hợp lệ.");
            if (field.Value.Editor != "image") values[field.Key] = value;
        }

        SectionItem? item = null;
        if (model.Id.HasValue)
        {
            item = await db.SectionItems.Include(x => x.MediaAsset)
                .FirstOrDefaultAsync(x => x.Id == model.Id && x.SectionKey == slot.SectionKey);
            if (item is null) return NotFound();
            model.MediaAsset = item.MediaAsset;
        }
        if (itemSchema.Fields.Values.Any(x => x.Editor == "image" && x.Required) &&
            model.MediaFile is not { Length: > 0 } && item?.MediaAssetId is null)
            ModelState.AddModelError(nameof(model.MediaFile), "Hình ảnh là bắt buộc.");
        if (!ModelState.IsValid) return View("EditItem", model);

        try
        {
            if (model.MediaFile is { Length: > 0 })
            {
                var asset = await mediaStorage.SaveImageAsync(model.MediaFile,
                    User.FindFirstValue(ClaimTypes.NameIdentifier), ImageUploadProfile.SectionImage, HttpContext.RequestAborted);
                model.MediaAsset = asset;
                if (item is not null) item.MediaAssetId = asset.Id;
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.MediaFile), ex.Message); return View("EditItem", model);
        }

        var itemIsNew = item is null;
        if (item is null)
        {
            var nextOrder = (await db.SectionItems.Where(x => x.SectionKey == slot.SectionKey)
                .MaxAsync(x => (int?)x.SortOrder) ?? 0) + 10;
            item = new SectionItem { SectionKey = slot.SectionKey, SortOrder = nextOrder, MediaAssetId = model.MediaAsset?.Id };
            db.SectionItems.Add(item);
        }
        item.ContentJson = JsonSerializer.Serialize(values);
        item.IsEnabled = model.IsEnabled;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (itemIsNew) await db.SaveChangesAsync(); // Lấy Id ổn định trước khi tạo EntityKey cho revision.
        AddSectionItemRevision(item, slot, itemIsNew ? "Created" : "Saved");
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã lưu mục nội dung.";
        return RedirectToAction(nameof(Items), new { id = slot.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveItem(long id, int direction)
    {
        var item = await db.SectionItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        var slot = await FindActiveSlotByKeyAsync(item.SectionKey);
        if (slot is null) return NotFound();
        var ordered = await db.SectionItems.Where(x => x.SectionKey == item.SectionKey)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        var index = ordered.FindIndex(x => x.Id == id); var target = index + Math.Sign(direction);
        if (index >= 0 && target >= 0 && target < ordered.Count)
        {
            (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
            for (var i = 0; i < ordered.Count; i++) ordered[i].SortOrder = (i + 1) * 10;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Items), new { id = slot.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(long id)
    {
        var item = await db.SectionItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        var slot = await FindActiveSlotByKeyAsync(item.SectionKey);
        if (slot is null) return NotFound();
        db.SectionItems.Remove(item); await db.SaveChangesAsync();
        TempData["Message"] = "Đã xóa mục nội dung. File ảnh trong Media Library vẫn được giữ lại.";
        return RedirectToAction(nameof(Items), new { id = slot.Id });
    }

    [HttpGet]
    public async Task<IActionResult> History(int id, long? itemId)
    {
        var slot = await FindActiveSlotAsync(id);
        if (slot is null) return NotFound();

        SectionItem? item = null;
        var entityType = "SectionContent";
        var entityKey = slot.SectionKey;
        if (itemId.HasValue)
        {
            item = await db.SectionItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == itemId && x.SectionKey == slot.SectionKey);
            if (item is null) return NotFound();
            entityType = "SectionItem";
            entityKey = GetSectionItemRevisionKey(slot.SectionKey, item.Id);
        }

        var revisions = await db.ContentRevisions.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityKey == entityKey)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Take(100).ToListAsync();
        return View(new RevisionHistoryViewModel(slot, item, revisions));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreRevision(long revisionId, int sectionId, long? itemId)
    {
        var slot = await FindActiveSlotAsync(sectionId);
        if (slot is null) return NotFound();
        var revision = await db.ContentRevisions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == revisionId);
        if (revision is null) return NotFound();

        try
        {
            if (itemId.HasValue)
            {
                var entityKey = GetSectionItemRevisionKey(slot.SectionKey, itemId.Value);
                if (revision.EntityType != "SectionItem" || revision.EntityKey != entityKey) return BadRequest();
                var item = await db.SectionItems.FirstOrDefaultAsync(x => x.Id == itemId && x.SectionKey == slot.SectionKey);
                if (item is null) return NotFound();
                var snapshot = JsonSerializer.Deserialize<SectionItemRevisionSnapshot>(revision.SnapshotJson)
                    ?? throw new JsonException("Revision không có dữ liệu hợp lệ.");
                if (snapshot.MediaAssetId.HasValue &&
                    !await db.MediaAssets.AnyAsync(x => x.Id == snapshot.MediaAssetId && !x.IsDeleted))
                    throw new InvalidOperationException("Ảnh của revision này không còn tồn tại trong Media Library.");

                item.ContentJson = snapshot.ContentJson;
                item.MediaAssetId = snapshot.MediaAssetId;
                item.IsEnabled = snapshot.IsEnabled;
                item.UpdatedAtUtc = DateTime.UtcNow;
                item.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                AddSectionItemRevision(item, slot, $"Restored #{revision.Id}");
            }
            else
            {
                if (revision.EntityType != "SectionContent" || revision.EntityKey != slot.SectionKey) return BadRequest();
                var content = await db.SectionContents.FirstOrDefaultAsync(x => x.SectionKey == slot.SectionKey);
                if (content is null) return NotFound();
                var snapshot = JsonSerializer.Deserialize<SectionContentRevisionSnapshot>(revision.SnapshotJson)
                    ?? throw new JsonException("Revision không có dữ liệu hợp lệ.");

                content.ContentJson = snapshot.ContentJson;
                if (User.IsInRole(DbInitializer.SuperAdministrator)) slot.IsEnabled = snapshot.IsEnabled;
                content.UpdatedAtUtc = DateTime.UtcNow;
                content.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                AddSectionContentRevision(content, slot, $"Restored #{revision.Id}");
            }

            await db.SaveChangesAsync();
            TempData["Message"] = $"Đã khôi phục revision #{revision.Id}.";
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(History), new { id = sectionId, itemId });
    }

    private async Task PrepareForViewAsync(SectionContentEditViewModel model, TemplateSection slot)
    {
        model.SectionKey = slot.SectionKey; model.SectionType = slot.SectionDefinition.SectionType; model.DisplayName = slot.DisplayName;
        var contentField = sectionSchemas.GetField(slot.SectionDefinition.SchemaJson, "content");
        model.ContentEditor = contentField.Editor; model.ContentHtmlPolicy = contentField.HtmlPolicy;
        model.AllowedHtmlTags = htmlSanitizer.GetAllowedTags(contentField.HtmlPolicy);
        model.Backgrounds = await LoadBackgroundsAsync(slot.SectionKey);
        model.GalleryImages = await LoadMediaAsync(slot.SectionKey, "Gallery");
        model.HasItems = sectionSchemas.GetItems(slot.SectionDefinition.SchemaJson) is not null;
    }

    private async Task<IReadOnlyList<SectionMedia>> LoadBackgroundsAsync(string sectionKey) => await db.SectionMedia.AsNoTracking()
        .Include(x => x.MediaAsset).Where(x => x.SectionKey == sectionKey && x.Role == "Background")
        .OrderBy(x => x.SortOrder).ToListAsync();

    private async Task<IReadOnlyList<SectionMedia>> LoadMediaAsync(string sectionKey, string role) => await db.SectionMedia.AsNoTracking()
        .Include(x => x.MediaAsset).Where(x => x.SectionKey == sectionKey && x.Role == role)
        .OrderBy(x => x.SortOrder).ToListAsync();

    private async Task<TemplateSection?> FindActiveSlotAsync(int id)
    {
        var activeTemplateId = (await db.SiteTemplateSettings.AsNoTracking().FirstAsync()).ActiveTemplateId;
        return await db.TemplateSections.Include(x => x.SectionDefinition)
            .FirstOrDefaultAsync(x => x.Id == id && x.TemplateId == activeTemplateId);
    }

    private async Task<TemplateSection?> FindActiveSlotByKeyAsync(string sectionKey)
    {
        var activeTemplateId = (await db.SiteTemplateSettings.AsNoTracking().FirstAsync()).ActiveTemplateId;
        return await db.TemplateSections.Include(x => x.SectionDefinition)
            .FirstOrDefaultAsync(x => x.SectionKey == sectionKey && x.TemplateId == activeTemplateId);
    }

    private static Dictionary<string, string?> DeserializeItemValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.OrdinalIgnoreCase);
        try
        {
            return new Dictionary<string, string?>(
                JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new(), StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException) { return new(StringComparer.OrdinalIgnoreCase); }
    }

    private IReadOnlyDictionary<string, IReadOnlySet<string>> GetAllowedItemTags(SectionItemsSchema schema) =>
        schema.Fields.Where(x => x.Value.Editor == "html").ToDictionary(
            x => x.Key, x => htmlSanitizer.GetAllowedTags(x.Value.HtmlPolicy), StringComparer.OrdinalIgnoreCase);

    private void AddSectionContentRevision(SectionContent content, TemplateSection slot, string action)
    {
        db.ContentRevisions.Add(new ContentRevision
        {
            EntityType = "SectionContent", EntityKey = slot.SectionKey, Action = action,
            DisplayName = slot.DisplayName,
            SnapshotJson = JsonSerializer.Serialize(new SectionContentRevisionSnapshot(content.ContentJson, slot.IsEnabled)),
            CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier), CreatedByName = User.Identity?.Name
        });
    }

    private void AddSectionItemRevision(SectionItem item, TemplateSection slot, string action)
    {
        db.ContentRevisions.Add(new ContentRevision
        {
            EntityType = "SectionItem", EntityKey = GetSectionItemRevisionKey(slot.SectionKey, item.Id), Action = action,
            DisplayName = slot.DisplayName,
            SnapshotJson = JsonSerializer.Serialize(new SectionItemRevisionSnapshot(
                item.ContentJson, item.MediaAssetId, item.IsEnabled)),
            CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier), CreatedByName = User.Identity?.Name
        });
    }

    private static string GetSectionItemRevisionKey(string sectionKey, long itemId) => $"{sectionKey}:{itemId}";
    private sealed record SectionContentRevisionSnapshot(string ContentJson, bool IsEnabled);
    private sealed record SectionItemRevisionSnapshot(string ContentJson, long? MediaAssetId, bool IsEnabled);
}
