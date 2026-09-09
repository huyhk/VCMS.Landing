using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using LandingCms.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Localization;
using LandingCms;
using System.Globalization;
using VNS.Licensing.Client.AspNetCore;

namespace LandingCms.Controllers;
public class HomeController(ApplicationDbContext db, IContactEmailSender emailSender, ILogger<HomeController> logger,
    IContentHtmlSanitizer htmlSanitizer, ISectionSchemaService sectionSchemas,
    ICloudflareTurnstileValidator turnstileValidator, IOptions<CloudflareTurnstileOptions> turnstileOptions,
    IThemeCssService themeCss, IStringLocalizer<PublicResource> publicText, IChromeLayoutService chromeLayouts,
    ILicenseState licenseState) : Controller
{
    public async Task<IActionResult> Index(string? culture)
    {
        var languages = await db.ContentLanguages.AsNoTracking()
            .Where(x => x.IsEnabled).OrderBy(x => x.SortOrder).ToListAsync();
        var defaultLanguage = languages.FirstOrDefault(x => x.IsDefault)
            ?? throw new InvalidOperationException("Chưa cấu hình ngôn ngữ mặc định.");
        var currentLanguage = string.IsNullOrWhiteSpace(culture)
            ? defaultLanguage
            : languages.FirstOrDefault(x => x.Code == culture.ToLowerInvariant());
        if (currentLanguage is null) return NotFound();
        var requestCulture = CultureInfo.GetCultureInfo(currentLanguage.Code);
        CultureInfo.CurrentCulture = requestCulture;
        CultureInfo.CurrentUICulture = requestCulture;
        ViewData["ContentLanguageCode"] = currentLanguage.Code;
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync();
        if (!currentLanguage.IsDefault)
        {
            var settingsTranslation = await db.SiteSettingTranslations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.SiteSettingId == settings.Id && x.LanguageCode == currentLanguage.Code);
            if (settingsTranslation is not null)
            {
                settings.SiteName = settingsTranslation.SiteName;
                settings.CompanyName = settingsTranslation.CompanyName;
                settings.LogoText = settingsTranslation.LogoText;
                settings.SeoTitle = settingsTranslation.SeoTitle;
                settings.SeoDescription = settingsTranslation.SeoDescription;
                settings.SeoKeywords = settingsTranslation.SeoKeywords;
                settings.Address = settingsTranslation.Address;
                settings.FooterText = settingsTranslation.FooterText;
            }
        }
        ViewData["Title"] = string.IsNullOrWhiteSpace(settings.SeoTitle) ? settings.SiteName : settings.SeoTitle;
        ViewData["Description"] = settings.SeoDescription;
        ViewData["Keywords"] = settings.SeoKeywords;
        var canonicalRoot = ResolveCanonicalRoot(licenseState.Current.CanonicalUrl);
        ViewData["CanonicalUrl"] = BuildLanguageUrl(canonicalRoot, currentLanguage, defaultLanguage);
        ViewData["AlternateLanguages"] = languages.ToDictionary(
            language => language.Code,
            language => BuildLanguageUrl(canonicalRoot, language, defaultLanguage));
        ViewData["DefaultLanguageUrl"] = canonicalRoot;
        var activeTheme = await db.SiteThemeSettings.AsNoTracking().Include(x => x.ActiveTheme).FirstAsync();
        ViewData["ThemeCss"] = themeCss.BuildCss(activeTheme.ActiveTheme.TokensJson);
        ViewData["ThemeKey"] = activeTheme.ActiveTheme.Key;
        var templateSetting = await db.SiteTemplateSettings.AsNoTracking()
            .Include(x => x.ActiveTemplate).FirstAsync();
        var slots = await db.TemplateSections.AsNoTracking()
            .Include(x => x.SectionDefinition)
            .Where(x => x.TemplateId == templateSetting.ActiveTemplateId && x.IsEnabled)
            .OrderBy(x => x.SortOrder).ToListAsync();
        var keys = slots.Select(x => x.SectionKey).ToArray();
        var contents = await db.SectionContents.AsNoTracking()
            .Where(x => keys.Contains(x.SectionKey))
            .ToDictionaryAsync(x => x.SectionKey);
        var contentIds = contents.Values.Select(x => x.Id).ToArray();
        var contentTranslations = currentLanguage.IsDefault
            ? new Dictionary<int, string>()
            : await db.SectionContentTranslations.AsNoTracking()
                .Where(x => contentIds.Contains(x.SectionContentId) && x.LanguageCode == currentLanguage.Code)
                .ToDictionaryAsync(x => x.SectionContentId, x => x.ContentJson);
        var sections = new List<LandingSection>();
        foreach (var slot in slots)
        {
            if (!contents.TryGetValue(slot.SectionKey, out var content)) continue;
            var contentJson = contentTranslations.GetValueOrDefault(content.Id) ?? content.ContentJson;
            var payload = JsonSerializer.Deserialize<SectionContentPayload>(contentJson) ?? new();
            var contentField = sectionSchemas.GetField(slot.SectionDefinition.SchemaJson, "content");
            var contentIsHtml = contentField.Editor == "html";
            if (contentIsHtml)
                payload.Content = htmlSanitizer.Sanitize(payload.Content, contentField.HtmlPolicy);
            sections.Add(new LandingSection
            {
                SectionKey = slot.SectionKey, SectionType = slot.SectionDefinition.SectionType,
                Eyebrow = payload.Eyebrow, Title = payload.Title, Subtitle = payload.Subtitle,
                Content = payload.Content, ContentIsHtml = contentIsHtml, ImageUrl = payload.ImageUrl,
                LayoutVariant = sectionSchemas.ResolveSetting(slot.SectionDefinition.SchemaJson, slot.SettingsJson, "layout")
                    ?? (slot.SectionDefinition.SectionType == "Content" ? "image-left"
                        : slot.SectionDefinition.SectionType == "Cards" ? "text-cards" : "default"),
                PrimaryButtonText = payload.PrimaryButtonText, PrimaryButtonUrl = payload.PrimaryButtonUrl,
                SecondaryButtonText = payload.SecondaryButtonText, SecondaryButtonUrl = payload.SecondaryButtonUrl,
                SortOrder = slot.SortOrder, IsPublished = true
            });
        }
        var slotIds = slots.Select(x => x.Id).ToArray();
        var navigationTranslations = currentLanguage.IsDefault
            ? new Dictionary<int, string?>()
            : await db.TemplateSectionTranslations.AsNoTracking()
                .Where(x => x.LanguageCode == currentLanguage.Code && slotIds.Contains(x.TemplateSectionId))
                .ToDictionaryAsync(x => x.TemplateSectionId, x => x.NavigationLabel);
        var renderedKeys = sections.Select(x => x.SectionKey).ToHashSet(StringComparer.Ordinal);
        var navigationItems = slots
            .Where(x => renderedKeys.Contains(x.SectionKey) && x.ShowInNavigation && sectionSchemas.GetNavigation(x.SectionDefinition.SchemaJson).Allowed)
            .Select(x => new NavigationItem(x.SectionKey,
                navigationTranslations.GetValueOrDefault(x.Id)
                ?? (string.IsNullOrWhiteSpace(x.NavigationLabel) ? x.DisplayName : x.NavigationLabel)))
            .ToList();
        var viewPath = templateSetting.ActiveTemplate.ViewPath;
        if (!viewPath.StartsWith("~/Views/Templates/", StringComparison.Ordinal) ||
            !viewPath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) || viewPath.Contains(".."))
            return Problem("Template view path is invalid.");
        var extendedSettings = await db.TemplateSettings.AsNoTracking()
            .Where(x => x.TemplateId == templateSetting.ActiveTemplateId && x.SettingDefinition.IsEnabled)
            .Select(x => new
            {
                x.SettingDefinition.Key,
                Value = x.SettingDefinition.Value != null && x.SettingDefinition.Value.Value != null
                    ? x.SettingDefinition.Value.Value
                    : x.OverrideDefaultValue ?? x.SettingDefinition.DefaultValue
            })
            .Where(x => x.Value != null && x.Value != "")
            .ToDictionaryAsync(x => x.Key, x => x.Value!);
        if (extendedSettings.TryGetValue("analytics.gtm_container_id", out var gtmContainerId) &&
            System.Text.RegularExpressions.Regex.IsMatch(gtmContainerId, "^GTM-[A-Z0-9]+$"))
            ViewData["GtmContainerId"] = gtmContainerId;
        var brandingIds = extendedSettings.Where(x => x.Key.StartsWith("branding.") && long.TryParse(x.Value, out _))
            .ToDictionary(x => x.Key, x => long.Parse(x.Value));
        var brandingAssetIds = brandingIds.Values.ToArray();
        var brandingAssets = await db.MediaAssets.AsNoTracking().Where(x => brandingAssetIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id);
        var brandingMedia = brandingIds.Where(x => brandingAssets.ContainsKey(x.Value)).ToDictionary(x => x.Key, x => brandingAssets[x.Value]);
        if (brandingMedia.TryGetValue("branding.favicon", out var favicon))
            ViewData["FaviconUrl"] = favicon.RelativeUrl;
        if (brandingMedia.TryGetValue("branding.logo_primary", out var sharingImage))
            ViewData["SharingImageUrl"] = new Uri(new Uri(canonicalRoot), sharingImage.RelativeUrl).ToString();
        var sectionMediaCandidates = await db.SectionMedia.AsNoTracking().Include(x => x.MediaAsset)
            .Where(x => keys.Contains(x.SectionKey) && x.IsEnabled && !x.MediaAsset.IsDeleted
                && (x.LanguageCode == null || x.LanguageCode == currentLanguage.Code))
            .OrderBy(x => x.SortOrder).ToListAsync();
        var sectionMediaRows = sectionMediaCandidates
            .GroupBy(x => new { x.SectionKey, x.Role })
            .SelectMany(group => !currentLanguage.IsDefault && group.Any(x => x.LanguageCode == currentLanguage.Code)
                ? group.Where(x => x.LanguageCode == currentLanguage.Code)
                : group.Where(x => x.LanguageCode == null))
            .ToList();
        var sectionsWithoutMainImage = sections
            .Where(section => !sectionMediaRows.Any(media => media.SectionKey == section.SectionKey && media.Role == "MainImage")
                && !string.IsNullOrWhiteSpace(section.ImageUrl)
                && section.ImageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (sectionsWithoutMainImage.Count > 0)
        {
            var legacyUrls = sectionsWithoutMainImage.Select(x => x.ImageUrl!).Distinct().ToArray();
            var legacyAssets = await db.MediaAssets.AsNoTracking()
                .Where(x => legacyUrls.Contains(x.RelativeUrl) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.RelativeUrl, StringComparer.OrdinalIgnoreCase);
            foreach (var section in sectionsWithoutMainImage)
            {
                if (!legacyAssets.TryGetValue(section.ImageUrl!, out var asset)) continue;
                sectionMediaRows.Add(new SectionMedia
                {
                    SectionKey = section.SectionKey,
                    MediaAssetId = asset.Id,
                    MediaAsset = asset,
                    Role = "MainImage",
                    SortOrder = 0
                });
            }
        }
        var sectionMedia = sectionMediaRows.GroupBy(x => x.SectionKey).ToDictionary(x => x.Key, x => (IReadOnlyList<SectionMedia>)x.ToList());
        var sectionItemRows = await db.SectionItems.AsNoTracking().Include(x => x.MediaAsset)
            .Where(x => keys.Contains(x.SectionKey) && x.IsEnabled)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        if (!currentLanguage.IsDefault && sectionItemRows.Count > 0)
        {
            var itemIds = sectionItemRows.Select(x => x.Id).ToArray();
            var itemTranslations = await db.SectionItemTranslations.AsNoTracking().Include(x => x.MediaAsset)
                .Where(x => itemIds.Contains(x.SectionItemId) && x.LanguageCode == currentLanguage.Code)
                .ToDictionaryAsync(x => x.SectionItemId);
            foreach (var item in sectionItemRows)
                if (itemTranslations.TryGetValue(item.Id, out var translated))
                {
                    item.ContentJson = translated.ContentJson;
                    if (translated.MediaAsset is not null)
                    {
                        item.MediaAsset = translated.MediaAsset;
                        item.MediaAssetId = translated.MediaAssetId;
                    }
                }
        }
        foreach (var item in sectionItemRows)
        {
            var slot = slots.First(x => x.SectionKey == item.SectionKey);
            var itemSchema = sectionSchemas.GetItems(slot.SectionDefinition.SchemaJson);
            if (itemSchema is null) continue;
            try
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, string?>>(item.ContentJson) ?? new();
                foreach (var field in itemSchema.Fields.Where(x => x.Value.Editor == "html"))
                    if (values.TryGetValue(field.Key, out var value)) values[field.Key] = htmlSanitizer.Sanitize(value, field.Value.HtmlPolicy);
                item.ContentJson = JsonSerializer.Serialize(values);
            }
            catch (JsonException) { item.ContentJson = "{}"; }
        }
        var sectionItems = sectionItemRows.GroupBy(x => x.SectionKey)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<SectionItem>)x.ToList());
        var headerLayout = chromeLayouts.ParseHeader(templateSetting.ActiveTemplate.HeaderLayoutJson);
        var footerLayout = chromeLayouts.ParseFooter(templateSetting.ActiveTemplate.FooterLayoutJson);
        var chromeMediaIds = headerLayout.Rows.Concat(footerLayout.Rows)
            .SelectMany(x => new long?[] { x.BackgroundMediaId, x.MobileBackgroundMediaId })
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var chromeMedia = await db.MediaAssets.AsNoTracking()
            .Where(x => chromeMediaIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id);
        ViewBag.PopupCampaign = await GetActivePopupAsync(currentLanguage, defaultLanguage);
        var turnstileSiteKey = turnstileOptions.Value.IsEnabled ? turnstileOptions.Value.SiteKey : null;
        return View(viewPath, new HomeViewModel(settings, sections, navigationItems, turnstileSiteKey, extendedSettings,
            brandingMedia, sectionMedia, sectionItems, languages, currentLanguage,
            headerLayout, footerLayout, chromeMedia));
    }

    private async Task<PopupRenderViewModel?> GetActivePopupAsync(ContentLanguage currentLanguage, ContentLanguage defaultLanguage)
    {
        var now = DateTime.UtcNow;
        var campaign = await db.PopupCampaigns.AsNoTracking()
            .Where(x => x.IsEnabled && (!x.StartAtUtc.HasValue || x.StartAtUtc <= now)
                && (!x.EndAtUtc.HasValue || x.EndAtUtc > now))
            .OrderByDescending(x => x.Priority).ThenByDescending(x => x.UpdatedAtUtc)
            .Include(x => x.Translations).ThenInclude(x => x.DesktopMedia)
            .Include(x => x.Translations).ThenInclude(x => x.MobileMedia)
            .FirstOrDefaultAsync();
        if (campaign is null) return null;
        var selected = campaign.Translations.FirstOrDefault(x => x.LanguageCode == currentLanguage.Code);
        var fallback = campaign.Translations.FirstOrDefault(x => x.LanguageCode == defaultLanguage.Code);
        if (selected is null && fallback is null) return null;
        var title = selected?.Title ?? fallback?.Title;
        var content = htmlSanitizer.Sanitize(selected?.ContentHtml ?? fallback?.ContentHtml, "BasicContent");
        var buttonText = selected?.ButtonText ?? fallback?.ButtonText;
        var rawUrl = selected?.ButtonUrl ?? fallback?.ButtonUrl;
        var url = string.IsNullOrWhiteSpace(rawUrl) ? null : PublicLinkUrl.Normalize(rawUrl);
        var desktopMedia = selected?.DesktopMedia ?? fallback?.DesktopMedia;
        var mobileMedia = selected?.MobileMedia ?? fallback?.MobileMedia;
        return new PopupRenderViewModel(campaign.Id, campaign.Version, currentLanguage.Code,
            title, content, buttonText, url,
            desktopMedia is { IsDeleted: false } ? desktopMedia : null,
            mobileMedia is { IsDeleted: false } ? mobileMedia : null,
            campaign.TriggerDelaySeconds, campaign.Frequency, campaign.IsDismissible);
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("contact")]
    public async Task<IActionResult> Contact(ContactFormViewModel model, string? culture)
    {
        var returnUrl = await GetContactReturnUrlAsync(culture);
        if (!string.IsNullOrWhiteSpace(model.Website)) return Redirect(returnUrl);
        if (!ModelState.IsValid) { TempData["ContactError"] = publicText["Contact.ValidationError"].Value; return Redirect(returnUrl); }
        var turnstileToken = Request.Form["cf-turnstile-response"].ToString();
        if (!await turnstileValidator.ValidateAsync(turnstileToken, HttpContext.RequestAborted))
        {
            TempData["ContactError"] = publicText["Contact.VerificationError"].Value;
            return Redirect(returnUrl);
        }
        var submission = new ContactSubmission
        {
            Name = model.Name.Trim(), Email = model.Email.Trim(), Phone = model.Phone?.Trim(), Message = model.Message.Trim(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()[..Math.Min(Request.Headers.UserAgent.ToString().Length, 500)]
        };
        db.ContactSubmissions.Add(submission);
        await db.SaveChangesAsync();
        try
        {
            var recipient = (await db.SiteSettings.AsNoTracking().FirstAsync()).Email ?? "";
            await emailSender.SendAsync(submission, recipient);
            submission.Status = "Sent"; submission.SentAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            submission.Status = "Failed"; submission.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 1000)];
            logger.LogError(ex, "Could not send contact submission {SubmissionId}", submission.Id);
        }
        await db.SaveChangesAsync();
        TempData["ContactSuccess"] = publicText["Contact.Success"].Value;
        return Redirect(returnUrl);
    }
    private async Task<string> GetContactReturnUrlAsync(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) return "/#contact";
        var language = await db.ContentLanguages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == culture.ToLowerInvariant() && x.IsEnabled);
        if (language is not null)
        {
            var requestCulture = CultureInfo.GetCultureInfo(language.Code);
            CultureInfo.CurrentCulture = requestCulture;
            CultureInfo.CurrentUICulture = requestCulture;
        }
        return language is null || language.IsDefault ? "/#contact" : $"/{language.Code}/#contact";
    }
    public IActionResult Error() => View();

    private string ResolveCanonicalRoot(string? configured)
    {
        if (Uri.TryCreate(configured, UriKind.Absolute, out var canonical))
            return canonical.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/";
        return $"{Request.Scheme}://{Request.Host}/";
    }

    private static string BuildLanguageUrl(string root, ContentLanguage language, ContentLanguage defaultLanguage) =>
        language.Code == defaultLanguage.Code ? root : $"{root}{language.Code}/";
}
