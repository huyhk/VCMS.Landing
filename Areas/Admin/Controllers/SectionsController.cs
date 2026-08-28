using System.Security.Claims;
using System.Text.Json;
using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator,Editor")]
public class SectionsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var slots = await db.TemplateSections.AsNoTracking().Include(x => x.Template).Include(x => x.SectionDefinition)
            .Where(x => x.TemplateId == setting.ActiveTemplateId).OrderBy(x => x.SortOrder).ToListAsync();
        var keys = slots.Select(x => x.SectionKey).ToArray();
        var contents = await db.SectionContents.AsNoTracking().Where(x => keys.Contains(x.SectionKey)).ToDictionaryAsync(x => x.SectionKey);
        ViewBag.TemplateName = slots.FirstOrDefault()?.Template.Name ?? "";
        return View(slots.Select(x => new SectionListItemViewModel(x, contents.GetValueOrDefault(x.SectionKey))).ToList());
    }

    public async Task<IActionResult> Edit(int id)
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var slot = await db.TemplateSections.AsNoTracking().Include(x => x.SectionDefinition)
            .FirstOrDefaultAsync(x => x.Id == id && x.TemplateId == setting.ActiveTemplateId);
        if (slot is null) return NotFound();
        var content = await db.SectionContents.AsNoTracking().FirstOrDefaultAsync(x => x.SectionKey == slot.SectionKey);
        var payload = content is null ? new SectionContentPayload() : JsonSerializer.Deserialize<SectionContentPayload>(content.ContentJson) ?? new();
        return View(new SectionContentEditViewModel
        {
            TemplateSectionId = slot.Id, ContentId = content?.Id, SectionKey = slot.SectionKey,
            SectionType = slot.SectionDefinition.SectionType, DisplayName = slot.DisplayName,
            Eyebrow = payload.Eyebrow, Title = payload.Title, Subtitle = payload.Subtitle, Content = payload.Content,
            ImageUrl = payload.ImageUrl, PrimaryButtonText = payload.PrimaryButtonText, PrimaryButtonUrl = payload.PrimaryButtonUrl,
            SecondaryButtonText = payload.SecondaryButtonText, SecondaryButtonUrl = payload.SecondaryButtonUrl,
            IsPublished = content?.IsPublished ?? slot.IsEnabledByDefault
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SectionContentEditViewModel model)
    {
        var setting = await db.SiteTemplateSettings.AsNoTracking().FirstAsync();
        var slot = await db.TemplateSections.Include(x => x.SectionDefinition)
            .FirstOrDefaultAsync(x => x.Id == model.TemplateSectionId && x.TemplateId == setting.ActiveTemplateId);
        if (slot is null) return NotFound();
        if (slot.IsRequired && !model.IsPublished) ModelState.AddModelError(nameof(model.IsPublished), "Section bắt buộc không thể bị ẩn.");
        if (!ModelState.IsValid) { model.SectionKey = slot.SectionKey; model.SectionType = slot.SectionDefinition.SectionType; model.DisplayName = slot.DisplayName; return View("Edit", model); }
        var content = await db.SectionContents.FirstOrDefaultAsync(x => x.SectionKey == slot.SectionKey);
        if (content is null) { content = new SectionContent { SectionKey = slot.SectionKey, SectionDefinitionId = slot.SectionDefinitionId }; db.SectionContents.Add(content); }
        content.ContentJson = JsonSerializer.Serialize(new SectionContentPayload
        {
            Eyebrow = model.Eyebrow, Title = model.Title, Subtitle = model.Subtitle, Content = model.Content,
            ImageUrl = model.ImageUrl, PrimaryButtonText = model.PrimaryButtonText, PrimaryButtonUrl = model.PrimaryButtonUrl,
            SecondaryButtonText = model.SecondaryButtonText, SecondaryButtonUrl = model.SecondaryButtonUrl
        });
        content.IsPublished = model.IsPublished; content.UpdatedAtUtc = DateTime.UtcNow;
        content.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã lưu {slot.DisplayName}.";
        return RedirectToAction(nameof(Index));
    }
}
