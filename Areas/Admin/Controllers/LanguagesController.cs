using LandingCms.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator")]
public sealed class LanguagesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var languages = await db.ContentLanguages.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
        return View(languages);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string code)
    {
        code = code.Trim().ToLowerInvariant();
        var language = await db.ContentLanguages.FirstOrDefaultAsync(x => x.Code == code);
        if (language is null) return NotFound();

        if (language.IsDefault && language.IsEnabled)
        {
            TempData["Error"] = "Không thể tắt ngôn ngữ mặc định.";
            return RedirectToAction(nameof(Index));
        }

        language.IsEnabled = !language.IsEnabled;
        await db.SaveChangesAsync();
        TempData["Message"] = language.IsEnabled
            ? $"Đã bật ngôn ngữ {language.Name}."
            : $"Đã tắt ngôn ngữ {language.Name}. Nội dung bản dịch vẫn được giữ lại.";
        return RedirectToAction(nameof(Index));
    }
}
