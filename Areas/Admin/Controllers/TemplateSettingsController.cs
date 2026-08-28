using System.Security.Claims;
using System.Text.RegularExpressions;
using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LandingCms.Services;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator")]
public partial class TemplateSettingsController(ApplicationDbContext db, IMediaStorageService mediaStorage) : Controller
{
    public async Task<IActionResult> Index() => View(await LoadModelAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Dictionary<int, string?> values)
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var definitions = await db.TemplateSettings.Include(x => x.SettingDefinition)
            .Where(x => x.TemplateId == setting.ActiveTemplateId).Select(x => x.SettingDefinition).ToListAsync();
        foreach (var definition in definitions)
        {
            values.TryGetValue(definition.Id, out var value); value = value?.Trim();
            if ((definition.IsRequired || await db.TemplateSettings.AnyAsync(x => x.TemplateId == setting.ActiveTemplateId && x.SettingDefinitionId == definition.Id && x.IsRequired)) && string.IsNullOrWhiteSpace(value))
                ModelState.AddModelError($"values[{definition.Id}]", $"{definition.Name} là bắt buộc.");
            if (!string.IsNullOrWhiteSpace(value) && !IsValid(definition, value))
                ModelState.AddModelError($"values[{definition.Id}]", $"Giá trị {definition.Name} không hợp lệ.");
        }
        if (!ModelState.IsValid) return View("Index", await LoadModelAsync(values));
        var ids = definitions.Select(x => x.Id).ToArray();
        var stored = await db.SettingValues.Where(x => ids.Contains(x.SettingDefinitionId)).ToDictionaryAsync(x => x.SettingDefinitionId);
        foreach (var definition in definitions.Where(x => x.ValueType == "Image"))
        {
            var file = Request.Form.Files.FirstOrDefault(x => x.Name == $"images[{definition.Id}]");
            if (file is not null && file.Length > 0)
            {
                try
                {
                    var profile = definition.Key == "branding.favicon" ? ImageUploadProfile.Favicon : ImageUploadProfile.Logo;
                    var asset = await mediaStorage.SaveImageAsync(file, User.FindFirstValue(ClaimTypes.NameIdentifier), profile, HttpContext.RequestAborted);
                    values[definition.Id] = asset.Id.ToString();
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError($"images[{definition.Id}]", ex.Message);
                }
            }
            else if (stored.TryGetValue(definition.Id, out var current)) values[definition.Id] = current.Value;
        }
        if (!ModelState.IsValid) return View("Index", await LoadModelAsync(values));
        foreach (var definition in definitions)
        {
            values.TryGetValue(definition.Id, out var value); value = value?.Trim();
            if (!stored.TryGetValue(definition.Id, out var item)) { item = new SettingValue { SettingDefinitionId = definition.Id }; db.SettingValues.Add(item); }
            item.Value = value; item.UpdatedAtUtc = DateTime.UtcNow; item.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã lưu các giá trị setting.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SettingsEditViewModel> LoadModelAsync(IReadOnlyDictionary<int, string?>? posted = null)
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().Include(x => x.ActiveTemplate).FirstAsync();
        var links = await db.TemplateSettings.AsNoTracking().Include(x => x.SettingDefinition).ThenInclude(x => x.Value)
            .Where(x => x.TemplateId == setting.ActiveTemplateId && x.SettingDefinition.IsEnabled).OrderBy(x => x.SortOrder).ToListAsync();
        var mediaIds = links.Where(x => x.SettingDefinition.ValueType == "Image")
            .Select(x => long.TryParse(x.SettingDefinition.Value?.Value, out var id) ? id : 0).Where(x => x > 0).ToArray();
        var media = await db.MediaAssets.AsNoTracking().Where(x => mediaIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id);
        return new SettingsEditViewModel(setting.ActiveTemplate.Name, links.Select(x => new SettingEditItemViewModel(
            x.SettingDefinitionId, x.SettingDefinition.Key, x.OverrideLabel ?? x.SettingDefinition.Name,
            x.SettingDefinition.Description, x.SettingDefinition.Group, x.SettingDefinition.ValueType,
            x.IsRequired || x.SettingDefinition.IsRequired,
            posted is not null && posted.TryGetValue(x.SettingDefinitionId, out var postedValue) ? postedValue : x.SettingDefinition.Value?.Value,
            x.OverrideDefaultValue ?? x.SettingDefinition.DefaultValue, x.SortOrder,
            long.TryParse(x.SettingDefinition.Value?.Value, out var mediaId) && media.TryGetValue(mediaId, out var asset) ? asset.RelativeUrl : null)).ToList());
    }

    private static bool IsValid(SettingDefinition definition, string value) => definition.ValueType switch
    {
        "Url" => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "Email" => new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value),
        "Boolean" => bool.TryParse(value, out _),
        _ when definition.Key == "analytics.ga_measurement_id" => GaIdRegex().IsMatch(value),
        _ when definition.Key == "analytics.gtm_container_id" => GtmIdRegex().IsMatch(value),
        _ => value.Length <= 10000
    };

    [GeneratedRegex("^G-[A-Z0-9]{4,20}$", RegexOptions.IgnoreCase)] private static partial Regex GaIdRegex();
    [GeneratedRegex("^GTM-[A-Z0-9]{4,20}$", RegexOptions.IgnoreCase)] private static partial Regex GtmIdRegex();
}
