using LandingCms.Data;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator,Editor")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var slots = await db.TemplateSections.AsNoTracking().Include(x => x.Template).Include(x => x.SectionDefinition)
            .Where(x => x.TemplateId == setting.ActiveTemplateId).OrderBy(x => x.SortOrder).ToListAsync();
        var keys = slots.Select(x => x.SectionKey).ToArray();
        var contents = await db.SectionContents.AsNoTracking().Where(x => keys.Contains(x.SectionKey)).ToDictionaryAsync(x => x.SectionKey);
        return View(new DashboardViewModel(slots.FirstOrDefault()?.Template.Name ?? "", slots.Select(x => new SectionListItemViewModel(x, contents.GetValueOrDefault(x.SectionKey))).ToList()));
    }
}
