using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using LandingCms.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace LandingCms.Controllers;
public class HomeController(ApplicationDbContext db, IContactEmailSender emailSender, ILogger<HomeController> logger,
    IContentHtmlSanitizer htmlSanitizer, ISectionSchemaService sectionSchemas,
    ICloudflareTurnstileValidator turnstileValidator, IOptions<CloudflareTurnstileOptions> turnstileOptions,
    IThemeCssService themeCss) : Controller
{
    public async Task<IActionResult> Index()
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync();
        ViewData["Title"] = string.IsNullOrWhiteSpace(settings.SeoTitle) ? settings.SiteName : settings.SeoTitle;
        ViewData["Description"] = settings.SeoDescription;
        ViewData["Keywords"] = settings.SeoKeywords;
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
        var sections = new List<LandingSection>();
        foreach (var slot in slots)
        {
            if (!contents.TryGetValue(slot.SectionKey, out var content)) continue;
            var payload = JsonSerializer.Deserialize<SectionContentPayload>(content.ContentJson) ?? new();
            var contentField = sectionSchemas.GetField(slot.SectionDefinition.SchemaJson, "content");
            var contentIsHtml = contentField.Editor == "html";
            if (contentIsHtml)
                payload.Content = htmlSanitizer.Sanitize(payload.Content, contentField.HtmlPolicy);
            sections.Add(new LandingSection
            {
                SectionKey = slot.SectionKey, SectionType = slot.SectionDefinition.SectionType,
                Eyebrow = payload.Eyebrow, Title = payload.Title, Subtitle = payload.Subtitle,
                Content = payload.Content, ContentIsHtml = contentIsHtml, ImageUrl = payload.ImageUrl,
                LayoutVariant = sectionSchemas.ResolveSetting(slot.SectionDefinition.SchemaJson, slot.SettingsJson, "layout") ?? "image-left",
                PrimaryButtonText = payload.PrimaryButtonText, PrimaryButtonUrl = payload.PrimaryButtonUrl,
                SecondaryButtonText = payload.SecondaryButtonText, SecondaryButtonUrl = payload.SecondaryButtonUrl,
                SortOrder = slot.SortOrder, IsPublished = true
            });
        }
        var renderedKeys = sections.Select(x => x.SectionKey).ToHashSet(StringComparer.Ordinal);
        var navigationItems = slots
            .Where(x => renderedKeys.Contains(x.SectionKey) && x.ShowInNavigation && sectionSchemas.GetNavigation(x.SectionDefinition.SchemaJson).Allowed)
            .Select(x => new NavigationItem(x.SectionKey,
                string.IsNullOrWhiteSpace(x.NavigationLabel) ? x.DisplayName : x.NavigationLabel))
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
        var brandingIds = extendedSettings.Where(x => x.Key.StartsWith("branding.") && long.TryParse(x.Value, out _))
            .ToDictionary(x => x.Key, x => long.Parse(x.Value));
        var brandingAssetIds = brandingIds.Values.ToArray();
        var brandingAssets = await db.MediaAssets.AsNoTracking().Where(x => brandingAssetIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id);
        var brandingMedia = brandingIds.Where(x => brandingAssets.ContainsKey(x.Value)).ToDictionary(x => x.Key, x => brandingAssets[x.Value]);
        if (brandingMedia.TryGetValue("branding.favicon", out var favicon))
            ViewData["FaviconUrl"] = favicon.RelativeUrl;
        var sectionMediaRows = await db.SectionMedia.AsNoTracking().Include(x => x.MediaAsset)
            .Where(x => keys.Contains(x.SectionKey) && x.IsEnabled && !x.MediaAsset.IsDeleted)
            .OrderBy(x => x.SortOrder).ToListAsync();
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
        var turnstileSiteKey = turnstileOptions.Value.IsEnabled ? turnstileOptions.Value.SiteKey : null;
        return View(viewPath, new HomeViewModel(settings, sections, navigationItems, turnstileSiteKey, extendedSettings, brandingMedia, sectionMedia, sectionItems));
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("contact")]
    public async Task<IActionResult> Contact(ContactFormViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Website)) return Redirect("/#contact");
        if (!ModelState.IsValid) { TempData["ContactError"] = "Vui lòng kiểm tra lại thông tin liên hệ."; return Redirect("/#contact"); }
        var turnstileToken = Request.Form["cf-turnstile-response"].ToString();
        if (!await turnstileValidator.ValidateAsync(turnstileToken, HttpContext.RequestAborted))
        {
            TempData["ContactError"] = "Không thể xác minh yêu cầu. Vui lòng thử lại.";
            return Redirect("/#contact");
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
        TempData["ContactSuccess"] = "Cảm ơn bạn. Chúng tôi đã nhận được thông tin và sẽ liên hệ sớm.";
        return Redirect("/#contact");
    }
    public IActionResult Error() => View();
}
