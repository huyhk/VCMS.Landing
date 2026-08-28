using LandingCms.Data;
using LandingCms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Administrator,Editor")]
public class SectionsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.LandingSections.OrderBy(x => x.SortOrder).ToListAsync());
    public IActionResult Create() => View("Edit", new LandingSection { SortOrder = 100, IsPublished = true });
    public async Task<IActionResult> Edit(int id) => await db.LandingSections.FindAsync(id) is { } item ? View(item) : NotFound();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(LandingSection model)
    {
        if (!ModelState.IsValid) return View("Edit", model);
        if (model.Id == 0) db.Add(model);
        else
        {
            var item = await db.LandingSections.FindAsync(model.Id);
            if (item is null) return NotFound();
            db.Entry(item).CurrentValues.SetValues(model);
            item.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã lưu section.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.LandingSections.FindAsync(id);
        if (item is not null) { db.Remove(item); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
