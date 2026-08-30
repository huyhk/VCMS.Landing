using LandingCms.Data;
using LandingCms.Models;
using LandingCms.Services;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = DbInitializer.SuperAdministrator)]
public class ThemesController(ApplicationDbContext db, IThemeCssService themeCss) : Controller
{
    public async Task<IActionResult> Index()
    {
        var activeThemeId = (await db.SiteThemeSettings.AsNoTracking().FirstAsync()).ActiveThemeId;
        var themes = await db.ThemeDefinitions.AsNoTracking().Where(x => x.IsEnabled)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        return View(themes.Select(x => new ThemeListItemViewModel(
            x, x.Id == activeThemeId, themeCss.GetTokens(x.TokensJson))).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Clone(int id)
    {
        var source = await db.ThemeDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsEnabled);
        if (source is null) return NotFound();
        var model = ThemeEditorViewModel.From(source);
        var nextOrder = (await db.ThemeDefinitions.MaxAsync(x => (int?)x.SortOrder) ?? 0) + 10;
        var theme = new ThemeDefinition
        {
            Key = $"custom-{Guid.NewGuid():N}", Name = $"{source.Name} tùy chỉnh",
            Description = source.Description, TokensJson = JsonSerializer.Serialize(model.ToTokens()),
            Source = "Custom", IsReadOnly = false, IsEnabled = true,
            BaseThemeId = source.BaseThemeId ?? source.Id, SortOrder = nextOrder,
            CreatedBy = User.Identity?.Name, UpdatedBy = User.Identity?.Name
        };
        db.ThemeDefinitions.Add(theme);
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã tạo bản theme tùy chỉnh. Hãy điều chỉnh và lưu lại.";
        return RedirectToAction(nameof(Edit), new { id = theme.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var theme = await db.ThemeDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsEnabled);
        if (theme is null) return NotFound();
        if (theme.IsReadOnly) return RedirectToAction(nameof(Index));
        return View(ThemeEditorViewModel.From(theme));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ThemeEditorViewModel model, bool activate = false)
    {
        var theme = await db.ThemeDefinitions.FirstOrDefaultAsync(x => x.Id == model.Id && x.IsEnabled);
        if (theme is null) return NotFound();
        if (theme.IsReadOnly) return Forbid();
        if (!ModelState.IsValid) return View(model);
        theme.Name = model.Name.Trim();
        theme.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        theme.TokensJson = JsonSerializer.Serialize(model.ToTokens());
        theme.UpdatedBy = User.Identity?.Name;
        theme.UpdatedAtUtc = DateTime.UtcNow;
        if (activate)
        {
            var setting = await db.SiteThemeSettings.FirstAsync();
            setting.ActiveThemeId = theme.Id;
            setting.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        TempData["Message"] = activate
            ? $"Đã lưu và áp dụng theme {theme.Name}."
            : $"Đã lưu theme {theme.Name}. Website chưa thay đổi cho đến khi bấm Áp dụng.";
        return activate ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Edit), new { id = theme.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var theme = await db.ThemeDefinitions.FirstOrDefaultAsync(x => x.Id == id && x.IsEnabled);
        if (theme is null) return NotFound();
        var setting = await db.SiteThemeSettings.FirstAsync();
        setting.ActiveThemeId = theme.Id;
        setting.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã áp dụng theme {theme.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var theme = await db.ThemeDefinitions.FirstOrDefaultAsync(x => x.Id == id && x.IsEnabled);
        if (theme is null) return NotFound();
        if (theme.IsReadOnly) return Forbid();
        var setting = await db.SiteThemeSettings.AsNoTracking().FirstAsync();
        if (setting.ActiveThemeId == theme.Id)
        {
            TempData["Error"] = "Không thể xóa theme đang được áp dụng.";
            return RedirectToAction(nameof(Index));
        }
        theme.IsEnabled = false;
        theme.UpdatedBy = User.Identity?.Name;
        theme.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã xóa theme {theme.Name}.";
        return RedirectToAction(nameof(Index));
    }
}
