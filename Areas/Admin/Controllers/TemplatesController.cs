using LandingCms.Data;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator")]
public class TemplatesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var installed = await db.PageTemplates.AsNoTracking().Where(x => x.IsEnabled)
            .Include(x => x.Sections).OrderBy(x => x.Name).ToListAsync();
        var templates = installed.Select(x => new TemplateListItemViewModel(
            x, x.Id == setting.ActiveTemplateId, x.Id == setting.DraftTemplateId, x.Sections.Count)).ToList();
        return View(templates);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var template = await db.PageTemplates.FirstOrDefaultAsync(x => x.Id == id && x.IsEnabled);
        if (template is null) return NotFound();
        var setting = await db.SiteTemplateSettings.FirstAsync();
        setting.ActiveTemplateId = template.Id; setting.DraftTemplateId = null; setting.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã áp dụng template {template.Name}.";
        return RedirectToAction(nameof(Index));
    }
}
