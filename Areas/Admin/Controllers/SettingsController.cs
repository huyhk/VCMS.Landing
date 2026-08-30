using LandingCms.Data;
using LandingCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator")]
public class SettingsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.SiteSettings.FirstAsync());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SiteSetting model)
    {
        if (!ModelState.IsValid) return View("Index", model);
        var item = await db.SiteSettings.FirstAsync();
        var legacyPrimaryColor = item.PrimaryColor;
        db.Entry(item).CurrentValues.SetValues(model);
        item.PrimaryColor = legacyPrimaryColor;
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã lưu cấu hình website.";
        return RedirectToAction(nameof(Index));
    }
}
