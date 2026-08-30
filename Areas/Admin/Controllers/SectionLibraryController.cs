using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = DbInitializer.SuperAdministrator)]
public class SectionLibraryController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var sections = await db.PageSections.AsNoTracking()
            .Include(x => x.SectionDefinition).Include(x => x.TemplateSections)
            .OrderBy(x => x.IsArchived).ThenBy(x => x.DisplayName).ToListAsync();
        var contentKeys = await db.SectionContents.AsNoTracking().Select(x => x.SectionKey).ToHashSetAsync();
        return View(sections.Select(x => new PageSectionListItemViewModel(
            x, x.TemplateSections.Count, contentKeys.Contains(x.SectionKey))).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? definitionId)
    {
        var definitions = await LoadDefinitionsAsync();
        var selected = definitions.FirstOrDefault(x => x.Id == definitionId) ?? definitions.FirstOrDefault();
        if (selected is null) return Problem("Chưa có SectionDefinition khả dụng.");
        return View(new PageSectionCreateViewModel
        {
            SectionDefinitionId = selected.Id,
            DisplayName = selected.Name,
            Definitions = definitions
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PageSectionCreateViewModel model)
    {
        var definition = await db.SectionDefinitions.FirstOrDefaultAsync(x => x.Id == model.SectionDefinitionId && x.IsEnabled);
        if (definition is null) return NotFound();
        if (!ModelState.IsValid)
        {
            model.Definitions = await LoadDefinitionsAsync();
            return View(model);
        }

        var keyPrefix = definition.Key[..Math.Min(definition.Key.Length, 70)];
        string sectionKey;
        do sectionKey = $"{keyPrefix}-{Guid.NewGuid():N}"[..(keyPrefix.Length + 9)];
        while (await db.PageSections.AnyAsync(x => x.SectionKey == sectionKey));

        db.PageSections.Add(new PageSection
        {
            SectionKey = sectionKey,
            DisplayName = model.DisplayName.Trim(),
            SectionDefinitionId = definition.Id
        });
        db.SectionContents.Add(new SectionContent
        {
            SectionKey = sectionKey,
            SectionDefinitionId = definition.Id,
            ContentJson = "{}",
            IsPublished = true
        });
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã tạo section {model.DisplayName}. Section chưa được gắn vào template nào.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var section = await db.PageSections.Include(x => x.TemplateSections).FirstOrDefaultAsync(x => x.Id == id);
        if (section is null) return NotFound();
        if (section.TemplateSections.Count > 0)
        {
            TempData["Error"] = "Hãy gỡ section khỏi tất cả template trước khi lưu trữ.";
            return RedirectToAction(nameof(Index));
        }
        section.IsArchived = true;
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã lưu trữ section {section.DisplayName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var section = await db.PageSections.FirstOrDefaultAsync(x => x.Id == id);
        if (section is null) return NotFound();
        section.IsArchived = false;
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã khôi phục section {section.DisplayName}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<SectionDefinition>> LoadDefinitionsAsync() =>
        await db.SectionDefinitions.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.Name).ToListAsync();
}
