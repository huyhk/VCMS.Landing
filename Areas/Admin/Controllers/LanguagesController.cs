using LandingCms.Data;
using LandingCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator")]
public sealed class LanguagesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var languages = await db.ContentLanguages.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
        return View(languages);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return BadRequest();
        code = code.Trim().ToLowerInvariant();
        var language = await db.ContentLanguages.FirstOrDefaultAsync(x => x.Code == code);
        if (language is null) return NotFound();

        if (language.IsDefault && language.IsEnabled)
        {
            TempData["Error"] = "Không thể tắt ngôn ngữ mặc định.";
            return RedirectToAction(nameof(Index));
        }

        language.IsEnabled = !language.IsEnabled;
        await db.SaveChangesAsync();
        TempData["Message"] = language.IsEnabled
            ? $"Đã bật ngôn ngữ {language.Name}."
            : $"Đã tắt ngôn ngữ {language.Name}. Nội dung bản dịch vẫn được giữ lại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return BadRequest();
        code = code.Trim().ToLowerInvariant();

        await using var transaction = await db.Database.BeginTransactionAsync();
        var current = await db.ContentLanguages.FirstOrDefaultAsync(x => x.IsDefault);
        var next = await db.ContentLanguages.FirstOrDefaultAsync(x => x.Code == code);
        if (current is null || next is null) return NotFound();
        if (current.Code == next.Code) return RedirectToAction(nameof(Index));

        await SwapDefaultContentAsync(current.Code, next.Code);

        current.IsDefault = false;
        await db.SaveChangesAsync();
        next.IsDefault = true;
        next.IsEnabled = true;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Message"] = $"Đã đặt {next.Name} làm ngôn ngữ mặc định. Nội dung đã được hoán đổi an toàn.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SwapDefaultContentAsync(string oldCode, string newCode)
    {
        var now = DateTime.UtcNow;

        var contents = await db.SectionContents.ToListAsync();
        var contentIds = contents.Select(x => x.Id).ToArray();
        var contentTranslations = await db.SectionContentTranslations
            .Where(x => contentIds.Contains(x.SectionContentId) &&
                        (x.LanguageCode == oldCode || x.LanguageCode == newCode))
            .ToListAsync();
        foreach (var item in contents)
        {
            var oldValue = item.ContentJson;
            var promoted = contentTranslations.FirstOrDefault(x =>
                x.SectionContentId == item.Id && x.LanguageCode == newCode);
            item.ContentJson = promoted?.ContentJson ?? oldValue;
            UpsertSectionContentTranslation(contentTranslations, item.Id, oldCode, oldValue, now);
            if (promoted is not null) db.SectionContentTranslations.Remove(promoted);
        }

        var sectionItems = await db.SectionItems.ToListAsync();
        var itemIds = sectionItems.Select(x => x.Id).ToArray();
        var itemTranslations = await db.SectionItemTranslations
            .Where(x => itemIds.Contains(x.SectionItemId) &&
                        (x.LanguageCode == oldCode || x.LanguageCode == newCode))
            .ToListAsync();
        foreach (var item in sectionItems)
        {
            var oldValue = item.ContentJson;
            var promoted = itemTranslations.FirstOrDefault(x =>
                x.SectionItemId == item.Id && x.LanguageCode == newCode);
            item.ContentJson = promoted?.ContentJson ?? oldValue;
            UpsertSectionItemTranslation(itemTranslations, item.Id, oldCode, oldValue, now);
            if (promoted is not null) db.SectionItemTranslations.Remove(promoted);
        }

        var slots = await db.TemplateSections.ToListAsync();
        var slotIds = slots.Select(x => x.Id).ToArray();
        var navigationTranslations = await db.TemplateSectionTranslations
            .Where(x => slotIds.Contains(x.TemplateSectionId) &&
                        (x.LanguageCode == oldCode || x.LanguageCode == newCode))
            .ToListAsync();
        foreach (var slot in slots)
        {
            var oldValue = slot.NavigationLabel;
            var promoted = navigationTranslations.FirstOrDefault(x =>
                x.TemplateSectionId == slot.Id && x.LanguageCode == newCode);
            slot.NavigationLabel = promoted?.NavigationLabel ?? oldValue;
            UpsertNavigationTranslation(navigationTranslations, slot.Id, oldCode, oldValue);
            if (promoted is not null) db.TemplateSectionTranslations.Remove(promoted);
        }

        var settings = await db.SiteSettings.ToListAsync();
        var settingIds = settings.Select(x => x.Id).ToArray();
        var settingTranslations = await db.SiteSettingTranslations
            .Where(x => settingIds.Contains(x.SiteSettingId) &&
                        (x.LanguageCode == oldCode || x.LanguageCode == newCode))
            .ToListAsync();
        foreach (var setting in settings)
        {
            var snapshot = SiteSettingSnapshot.From(setting);
            var promoted = settingTranslations.FirstOrDefault(x =>
                x.SiteSettingId == setting.Id && x.LanguageCode == newCode);
            if (promoted is not null) Apply(setting, promoted);
            UpsertSiteSettingTranslation(settingTranslations, setting.Id, oldCode, snapshot, now);
            if (promoted is not null) db.SiteSettingTranslations.Remove(promoted);
        }

        await db.SaveChangesAsync();
    }

    private void UpsertSectionContentTranslation(List<SectionContentTranslation> translations,
        int id, string code, string value, DateTime now)
    {
        var item = translations.FirstOrDefault(x => x.SectionContentId == id && x.LanguageCode == code);
        if (item is null)
        {
            item = new SectionContentTranslation { SectionContentId = id, LanguageCode = code };
            db.SectionContentTranslations.Add(item);
            translations.Add(item);
        }
        item.ContentJson = value;
        item.UpdatedAtUtc = now;
    }

    private void UpsertSectionItemTranslation(List<SectionItemTranslation> translations,
        long id, string code, string value, DateTime now)
    {
        var item = translations.FirstOrDefault(x => x.SectionItemId == id && x.LanguageCode == code);
        if (item is null)
        {
            item = new SectionItemTranslation { SectionItemId = id, LanguageCode = code };
            db.SectionItemTranslations.Add(item);
            translations.Add(item);
        }
        item.ContentJson = value;
        item.UpdatedAtUtc = now;
    }

    private void UpsertNavigationTranslation(List<TemplateSectionTranslation> translations,
        int id, string code, string? value)
    {
        var item = translations.FirstOrDefault(x => x.TemplateSectionId == id && x.LanguageCode == code);
        if (item is null)
        {
            item = new TemplateSectionTranslation { TemplateSectionId = id, LanguageCode = code };
            db.TemplateSectionTranslations.Add(item);
            translations.Add(item);
        }
        item.NavigationLabel = value;
    }

    private void UpsertSiteSettingTranslation(List<SiteSettingTranslation> translations,
        int id, string code, SiteSettingSnapshot value, DateTime now)
    {
        var item = translations.FirstOrDefault(x => x.SiteSettingId == id && x.LanguageCode == code);
        if (item is null)
        {
            item = new SiteSettingTranslation { SiteSettingId = id, LanguageCode = code };
            db.SiteSettingTranslations.Add(item);
            translations.Add(item);
        }
        item.SiteName = value.SiteName;
        item.CompanyName = value.CompanyName;
        item.LogoText = value.LogoText;
        item.SeoTitle = value.SeoTitle;
        item.SeoDescription = value.SeoDescription;
        item.SeoKeywords = value.SeoKeywords;
        item.Address = value.Address;
        item.FooterText = value.FooterText;
        item.UpdatedAtUtc = now;
    }

    private static void Apply(SiteSetting setting, SiteSettingTranslation value)
    {
        setting.SiteName = value.SiteName;
        setting.CompanyName = value.CompanyName;
        setting.LogoText = value.LogoText;
        setting.SeoTitle = value.SeoTitle;
        setting.SeoDescription = value.SeoDescription;
        setting.SeoKeywords = value.SeoKeywords;
        setting.Address = value.Address;
        setting.FooterText = value.FooterText;
    }

    private sealed record SiteSettingSnapshot(string SiteName, string? CompanyName, string? LogoText,
        string? SeoTitle, string? SeoDescription, string? SeoKeywords, string? Address, string? FooterText)
    {
        public static SiteSettingSnapshot From(SiteSetting value) => new(value.SiteName, value.CompanyName,
            value.LogoText, value.SeoTitle, value.SeoDescription, value.SeoKeywords, value.Address, value.FooterText);
    }
}
