using LandingCms.Data;
using LandingCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LandingCms.ViewModels;

namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator")]
public class SettingsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? language)
    {
        var languages = await db.ContentLanguages.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.SortOrder).ToListAsync();
        var currentLanguage = ResolveLanguage(languages, language);
        if (currentLanguage is null) return NotFound();
        var item = await db.SiteSettings.AsNoTracking().FirstAsync();
        var translation = currentLanguage.IsDefault ? null : await db.SiteSettingTranslations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SiteSettingId == item.Id && x.LanguageCode == currentLanguage.Code);
        return View(new SiteSettingsEditViewModel
        {
            Id = item.Id, LanguageCode = currentLanguage.Code, Languages = languages,
            IsDefaultLanguage = currentLanguage.IsDefault, HasTranslation = currentLanguage.IsDefault || translation is not null,
            SiteName = translation?.SiteName ?? item.SiteName,
            CompanyName = translation?.CompanyName ?? item.CompanyName,
            LogoText = translation?.LogoText ?? item.LogoText,
            SeoTitle = translation?.SeoTitle ?? item.SeoTitle,
            SeoDescription = translation?.SeoDescription ?? item.SeoDescription,
            SeoKeywords = translation?.SeoKeywords ?? item.SeoKeywords,
            Address = translation?.Address ?? item.Address,
            FooterText = translation?.FooterText ?? item.FooterText,
            Phone = item.Phone, Email = item.Email
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SiteSettingsEditViewModel model)
    {
        var languages = await db.ContentLanguages.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.SortOrder).ToListAsync();
        var currentLanguage = ResolveLanguage(languages, model.LanguageCode);
        if (currentLanguage is null) return NotFound();
        model.Languages = languages; model.IsDefaultLanguage = currentLanguage.IsDefault;
        if (!ModelState.IsValid) return View("Index", model);
        var item = await db.SiteSettings.FirstAsync();
        if (currentLanguage.IsDefault)
        {
            item.SiteName = model.SiteName; item.CompanyName = model.CompanyName; item.LogoText = model.LogoText;
            item.SeoTitle = model.SeoTitle; item.SeoDescription = model.SeoDescription; item.SeoKeywords = model.SeoKeywords;
            item.Phone = model.Phone; item.Email = model.Email; item.Address = model.Address; item.FooterText = model.FooterText;
        }
        else
        {
            var translation = await db.SiteSettingTranslations
                .FirstOrDefaultAsync(x => x.SiteSettingId == item.Id && x.LanguageCode == currentLanguage.Code);
            if (translation is null)
            {
                translation = new SiteSettingTranslation { SiteSettingId = item.Id, LanguageCode = currentLanguage.Code };
                db.SiteSettingTranslations.Add(translation);
            }
            translation.SiteName = model.SiteName; translation.CompanyName = model.CompanyName; translation.LogoText = model.LogoText;
            translation.SeoTitle = model.SeoTitle; translation.SeoDescription = model.SeoDescription;
            translation.SeoKeywords = model.SeoKeywords; translation.Address = model.Address; translation.FooterText = model.FooterText;
            translation.UpdatedAtUtc = DateTime.UtcNow;
            translation.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã lưu cấu hình website ({currentLanguage.Name}).";
        return RedirectToAction(nameof(Index), new { language = currentLanguage.Code });
    }

    private static ContentLanguage? ResolveLanguage(IReadOnlyList<ContentLanguage> languages, string? code) =>
        string.IsNullOrWhiteSpace(code) ? languages.FirstOrDefault(x => x.IsDefault) :
            languages.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
}
