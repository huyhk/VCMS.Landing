using LandingCms.Data;
using LandingCms.Services;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "SuperAdministrator")]
public sealed class ChromeController(ApplicationDbContext db, IChromeLayoutService layouts) : Controller
{
    public async Task<IActionResult> Index()
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().Include(x => x.ActiveTemplate).FirstAsync();
        return View(await BuildModelAsync(setting.ActiveTemplate.Name,
            setting.ActiveTemplate.HeaderLayoutJson is "" or "{}" ? layouts.GetHeaderPreset("standard") : setting.ActiveTemplate.HeaderLayoutJson,
            setting.ActiveTemplate.FooterLayoutJson is "" or "{}" ? layouts.GetFooterPreset("corporate") : setting.ActiveTemplate.FooterLayoutJson));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ChromeBuilderViewModel model)
    {
        var setting = await db.SiteTemplateSettings.FirstAsync();
        var template = await db.PageTemplates.FirstAsync(x => x.Id == setting.ActiveTemplateId);
        try { template.HeaderLayoutJson = layouts.NormalizeHeader(model.HeaderJson); template.FooterLayoutJson = layouts.NormalizeFooter(model.FooterJson); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View("Index", await BuildModelAsync(template.Name, model.HeaderJson, model.FooterJson)); }
        await db.SaveChangesAsync(); TempData["Message"] = "Đã lưu cấu trúc Header/Footer."; return RedirectToAction(nameof(Index));
    }
    private async Task<ChromeBuilderViewModel> BuildModelAsync(string name, string header, string footer)
    {
        var languages = await db.ContentLanguages.AsNoTracking().Where(x => x.IsEnabled)
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.SortOrder)
            .Select(x => new ChromeLanguageOption(x.Code, x.Name, x.IsDefault)).ToListAsync();
        return new ChromeBuilderViewModel
        {
            TemplateName = name, HeaderJson = header, FooterJson = footer, Languages = languages,
            HeaderPresets = new() { ["standard"] = layouts.GetHeaderPreset("standard"), ["centered"] = layouts.GetHeaderPreset("centered"), ["minimal"] = layouts.GetHeaderPreset("minimal"), ["benka"] = layouts.GetHeaderPreset("benka") },
            FooterPresets = new() { ["corporate"] = layouts.GetFooterPreset("corporate"), ["minimal"] = layouts.GetFooterPreset("minimal"), ["benka"] = layouts.GetFooterPreset("benka") }
        };
    }
}
