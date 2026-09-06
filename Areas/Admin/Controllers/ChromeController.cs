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
        return View(BuildModel(setting.ActiveTemplate.Name,
            setting.ActiveTemplate.HeaderLayoutJson is "" or "{}" ? layouts.GetHeaderPreset("standard") : setting.ActiveTemplate.HeaderLayoutJson,
            setting.ActiveTemplate.FooterLayoutJson is "" or "{}" ? layouts.GetFooterPreset("corporate") : setting.ActiveTemplate.FooterLayoutJson));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ChromeBuilderViewModel model)
    {
        var setting = await db.SiteTemplateSettings.FirstAsync();
        var template = await db.PageTemplates.FirstAsync(x => x.Id == setting.ActiveTemplateId);
        try { template.HeaderLayoutJson = layouts.NormalizeHeader(model.HeaderJson); template.FooterLayoutJson = layouts.NormalizeFooter(model.FooterJson); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View("Index", BuildModel(template.Name, model.HeaderJson, model.FooterJson)); }
        await db.SaveChangesAsync(); TempData["Message"] = "Đã lưu cấu trúc Header/Footer."; return RedirectToAction(nameof(Index));
    }
    private ChromeBuilderViewModel BuildModel(string name, string header, string footer) => new()
    {
        TemplateName = name, HeaderJson = header, FooterJson = footer,
        HeaderPresets = new() { ["standard"] = layouts.GetHeaderPreset("standard"), ["centered"] = layouts.GetHeaderPreset("centered"), ["minimal"] = layouts.GetHeaderPreset("minimal"), ["benka"] = layouts.GetHeaderPreset("benka") },
        FooterPresets = new() { ["corporate"] = layouts.GetFooterPreset("corporate"), ["minimal"] = layouts.GetFooterPreset("minimal"), ["benka"] = layouts.GetFooterPreset("benka") }
    };
}
