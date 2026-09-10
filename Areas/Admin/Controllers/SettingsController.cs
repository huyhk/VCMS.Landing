using LandingCms.Data;
using LandingCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LandingCms.ViewModels;
using LandingCms.Services;

namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator")]
public class SettingsController(ApplicationDbContext db, IMediaStorageService mediaStorage) : Controller
{
    private static readonly string[] BrandingKeys = ["branding.logo_primary", "branding.logo_light", "branding.favicon"];

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
            Phone = item.Phone, Email = item.Email,
            BrandingImages = currentLanguage.IsDefault ? await LoadBrandingImagesAsync() : []
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SiteSettingsEditViewModel model)
    {
        var languages = await db.ContentLanguages.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.SortOrder).ToListAsync();
        var currentLanguage = ResolveLanguage(languages, model.LanguageCode);
        if (currentLanguage is null) return NotFound();
        model.Languages = languages; model.IsDefaultLanguage = currentLanguage.IsDefault;
        model.BrandingImages = currentLanguage.IsDefault ? await LoadBrandingImagesAsync() : [];
        if (!ModelState.IsValid) return View("Index", model);
        if (currentLanguage.IsDefault)
        {
            await SaveBrandingImagesAsync(model.BrandingImages);
            if (!ModelState.IsValid)
            {
                model.BrandingImages = await LoadBrandingImagesAsync();
                return View("Index", model);
            }
        }
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

    private async Task<IReadOnlyList<BrandingImageEditViewModel>> LoadBrandingImagesAsync()
    {
        var definitions = await db.SettingDefinitions.AsNoTracking().Include(x => x.Value)
            .Where(x => BrandingKeys.Contains(x.Key) && x.IsEnabled).OrderBy(x => x.SortOrder).ToListAsync();
        var mediaIds = definitions.Select(x => long.TryParse(x.Value?.Value, out var id) ? id : 0).Where(x => x > 0).ToArray();
        var media = await db.MediaAssets.AsNoTracking().Where(x => mediaIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id);
        return definitions.Select(x => new BrandingImageEditViewModel(
            x.Id, x.Key, x.Name, x.Description,
            long.TryParse(x.Value?.Value, out var mediaId) && media.TryGetValue(mediaId, out var asset) ? asset.RelativeUrl : null)).ToList();
    }

    private async Task SaveBrandingImagesAsync(IReadOnlyList<BrandingImageEditViewModel> images)
    {
        if (images.Count == 0) return;
        var ids = images.Select(x => x.DefinitionId).ToArray();
        var definitions = await db.SettingDefinitions.Where(x => ids.Contains(x.Id) && BrandingKeys.Contains(x.Key)).ToListAsync();
        var stored = await db.SettingValues.Where(x => ids.Contains(x.SettingDefinitionId)).ToDictionaryAsync(x => x.SettingDefinitionId);
        foreach (var definition in definitions)
        {
            var file = Request.Form.Files.FirstOrDefault(x => x.Name == $"brandingImages[{definition.Id}]");
            if (file is null || file.Length == 0) continue;
            try
            {
                var profile = definition.Key == "branding.favicon" ? ImageUploadProfile.Favicon : ImageUploadProfile.Logo;
                var asset = await mediaStorage.SaveImageAsync(file, User.FindFirstValue(ClaimTypes.NameIdentifier), profile, HttpContext.RequestAborted);
                if (!stored.TryGetValue(definition.Id, out var value))
                {
                    value = new SettingValue { SettingDefinitionId = definition.Id };
                    db.SettingValues.Add(value);
                    stored[definition.Id] = value;
                }
                value.Value = asset.Id.ToString();
                value.UpdatedAtUtc = DateTime.UtcNow;
                value.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError($"brandingImages[{definition.Id}]", ex.Message);
            }
        }
    }
}
