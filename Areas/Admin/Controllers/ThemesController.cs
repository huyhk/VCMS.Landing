using LandingCms.Data;
using LandingCms.Services;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
}
