using LandingCms.Data;
using LandingCms.Models;
using LandingCms.Services;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = DbInitializer.SuperAdministrator)]
public sealed class PopupCampaignsController(ApplicationDbContext db, IContentHtmlSanitizer htmlSanitizer) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await db.PopupCampaigns.AsNoTracking().Include(x => x.Translations)
            .OrderByDescending(x => x.IsEnabled).ThenByDescending(x => x.Priority).ThenByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(ct);
        return View(rows);
    }

    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var language = await db.ContentLanguages.AsNoTracking().FirstAsync(x => x.IsDefault && x.IsEnabled, ct);
        return View("Edit", await PopulateAsync(new PopupCampaignEditViewModel
        {
            LanguageCode = language.Code, StartAtLocal = DateTime.Now, IsDismissible = true
        }, ct));
    }

    public async Task<IActionResult> Edit(int id, string? language, CancellationToken ct)
    {
        var entity = await db.PopupCampaigns.AsNoTracking().Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        var languages = await LoadLanguagesAsync(ct);
        var selected = languages.FirstOrDefault(x => x.Code == language) ?? languages.First(x => x.IsDefault);
        var translation = entity.Translations.FirstOrDefault(x => x.LanguageCode == selected.Code);
        return View(await PopulateAsync(new PopupCampaignEditViewModel
        {
            Id = entity.Id, InternalName = entity.InternalName, IsEnabled = entity.IsEnabled,
            StartAtLocal = ToLocal(entity.StartAtUtc), EndAtLocal = ToLocal(entity.EndAtUtc),
            Priority = entity.Priority, TriggerDelaySeconds = entity.TriggerDelaySeconds,
            Frequency = entity.Frequency, IsDismissible = entity.IsDismissible,
            LanguageCode = selected.Code, Title = translation?.Title, ContentHtml = translation?.ContentHtml,
            ButtonText = translation?.ButtonText, ButtonUrl = translation?.ButtonUrl,
            DesktopMediaId = translation?.DesktopMediaId, MobileMediaId = translation?.MobileMediaId,
            HasTranslation = translation is not null
        }, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PopupCampaignEditViewModel model, CancellationToken ct)
    {
        var language = await db.ContentLanguages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == model.LanguageCode && x.IsEnabled, ct);
        if (language is null) ModelState.AddModelError(nameof(model.LanguageCode), "Ngôn ngữ không hợp lệ.");
        if (model.EndAtLocal.HasValue && model.StartAtLocal.HasValue && model.EndAtLocal <= model.StartAtLocal)
            ModelState.AddModelError(nameof(model.EndAtLocal), "Thời điểm kết thúc phải sau thời điểm bắt đầu.");
        if (model.IsEnabled && language?.IsDefault == true && string.IsNullOrWhiteSpace(model.Title) && !model.DesktopMediaId.HasValue)
            ModelState.AddModelError(nameof(model.Title), "Popup đang bật cần có tiêu đề hoặc hình desktop.");
        if (model.IsEnabled && language?.IsDefault == false && model.Id != 0)
        {
            var defaultCode = await db.ContentLanguages.AsNoTracking().Where(x => x.IsDefault).Select(x => x.Code).FirstAsync(ct);
            var hasDefaultContent = await db.PopupCampaignTranslations.AsNoTracking().AnyAsync(x =>
                x.PopupCampaignId == model.Id && x.LanguageCode == defaultCode
                && (!string.IsNullOrWhiteSpace(x.Title) || x.DesktopMediaId.HasValue), ct);
            if (!hasDefaultContent) ModelState.AddModelError("", "Hãy hoàn tất nội dung ngôn ngữ mặc định trước khi bật popup.");
        }
        var mediaIds = new[] { model.DesktopMediaId, model.MobileMediaId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        if (mediaIds.Length > 0 && await db.MediaAssets.CountAsync(x => mediaIds.Contains(x.Id) && !x.IsDeleted && x.ContentType.StartsWith("image/"), ct) != mediaIds.Length)
            ModelState.AddModelError(nameof(model.DesktopMediaId), "Media đã chọn không hợp lệ.");
        var buttonUrl = string.IsNullOrWhiteSpace(model.ButtonUrl) ? null : PublicLinkUrl.Normalize(model.ButtonUrl);
        if (!string.IsNullOrWhiteSpace(model.ButtonUrl) && buttonUrl is null)
            ModelState.AddModelError(nameof(model.ButtonUrl), "URL nút hành động không hợp lệ.");
        if (!new[] { "always", "session", "daily", "once" }.Contains(model.Frequency))
            ModelState.AddModelError(nameof(model.Frequency), "Tần suất không hợp lệ.");
        if (!ModelState.IsValid)
        {
            model.ContentHtml = htmlSanitizer.Sanitize(model.ContentHtml, "BasicContent");
            return View("Edit", await PopulateAsync(model, ct));
        }

        var entity = model.Id == 0 ? new PopupCampaign { CreatedAtUtc = DateTime.UtcNow } :
            await db.PopupCampaigns.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == model.Id, ct);
        if (entity is null) return NotFound();
        entity.InternalName = model.InternalName.Trim();
        entity.IsEnabled = model.IsEnabled;
        entity.StartAtUtc = ToUtc(model.StartAtLocal);
        entity.EndAtUtc = ToUtc(model.EndAtLocal);
        entity.Priority = model.Priority;
        entity.TriggerDelaySeconds = model.TriggerDelaySeconds;
        entity.Frequency = model.Frequency;
        entity.IsDismissible = model.IsDismissible;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        if (entity.Id != 0) entity.Version++;
        if (entity.Id == 0) db.PopupCampaigns.Add(entity);

        var translation = entity.Translations.FirstOrDefault(x => x.LanguageCode == model.LanguageCode);
        if (translation is null)
        {
            translation = new PopupCampaignTranslation { LanguageCode = model.LanguageCode };
            entity.Translations.Add(translation);
        }
        translation.Title = model.Title?.Trim();
        translation.ContentHtml = htmlSanitizer.Sanitize(model.ContentHtml, "BasicContent");
        translation.ButtonText = model.ButtonText?.Trim();
        translation.ButtonUrl = buttonUrl;
        translation.DesktopMediaId = model.DesktopMediaId;
        translation.MobileMediaId = model.MobileMediaId;
        await db.SaveChangesAsync(ct);
        TempData["Message"] = "Đã lưu popup và bản dịch đang chọn.";
        return RedirectToAction(nameof(Edit), new { id = entity.Id, language = model.LanguageCode });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var entity = await db.PopupCampaigns.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (!entity.IsEnabled)
        {
            var defaultCode = await db.ContentLanguages.AsNoTracking().Where(x => x.IsDefault).Select(x => x.Code).FirstAsync(ct);
            var content = entity.Translations.FirstOrDefault(x => x.LanguageCode == defaultCode);
            if (content is null || (string.IsNullOrWhiteSpace(content.Title) && !content.DesktopMediaId.HasValue))
            {
                TempData["Error"] = "Hãy hoàn tất nội dung ngôn ngữ mặc định trước khi bật popup.";
                return RedirectToAction(nameof(Index));
            }
        }
        entity.IsEnabled = !entity.IsEnabled;
        entity.Version++;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        TempData["Message"] = entity.IsEnabled ? "Đã bật popup." : "Đã tắt popup.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await db.PopupCampaigns.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        db.PopupCampaigns.Remove(entity);
        await db.SaveChangesAsync(ct);
        TempData["Message"] = "Đã xóa popup.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PopupCampaignEditViewModel> PopulateAsync(PopupCampaignEditViewModel model, CancellationToken ct)
    {
        model.Languages = await LoadLanguagesAsync(ct);
        model.Media = await db.MediaAssets.AsNoTracking()
            .Where(x => !x.IsDeleted && x.ContentType.StartsWith("image/"))
            .OrderByDescending(x => x.UploadedAtUtc).Take(500).ToListAsync(ct);
        return model;
    }

    private Task<List<ContentLanguage>> LoadLanguagesAsync(CancellationToken ct) => db.ContentLanguages.AsNoTracking()
        .Where(x => x.IsEnabled).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct);

    private static DateTime? ToUtc(DateTime? value) => value.HasValue
        ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified), WebsiteTimeZone)
        : null;
    private static DateTime? ToLocal(DateTime? value) => value.HasValue
        ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc), WebsiteTimeZone)
        : null;
    private static readonly TimeZoneInfo WebsiteTimeZone = ResolveWebsiteTimeZone();
    private static TimeZoneInfo ResolveWebsiteTimeZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch (TimeZoneNotFoundException) { }
        return TimeZoneInfo.Utc;
    }
}
