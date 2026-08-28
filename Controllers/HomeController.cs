using LandingCms.Data;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Controllers;
public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync();
        var sections = await db.LandingSections.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ToListAsync();
        return View(new HomeViewModel(settings, sections));
    }
    public IActionResult Error() => View();
}
