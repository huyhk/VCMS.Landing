using LandingCms.Data;
using LandingCms.Models;
using LandingCms.Services;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator")]
public class TemplatesController(ApplicationDbContext db, ISectionSchemaService sectionSchemas) : Controller
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

    public async Task<IActionResult> Sections(int id)
    {
        var template = await db.PageTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsEnabled);
        if (template is null) return NotFound();
        var activeTemplateId = (await db.SiteTemplateSettings.AsNoTracking().FirstAsync()).ActiveTemplateId;
        var sections = await db.TemplateSections.AsNoTracking().Include(x => x.SectionDefinition)
            .Where(x => x.TemplateId == id).OrderBy(x => x.SortOrder).ToListAsync();
        return View(new TemplateComposerViewModel(template, sections, id == activeTemplateId));
    }

    [HttpGet]
    public async Task<IActionResult> CreateSection(int templateId, int? definitionId)
    {
        if (!await db.PageTemplates.AnyAsync(x => x.Id == templateId && x.IsEnabled)) return NotFound();
        var definitions = await db.SectionDefinitions.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.Name).ToListAsync();
        var selected = definitions.FirstOrDefault(x => x.Id == definitionId) ?? definitions.FirstOrDefault();
        if (selected is null) return Problem("Chưa có SectionDefinition khả dụng.");
        return View(await PrepareComposerModelAsync(new TemplateSectionComposerViewModel
        {
            TemplateId = templateId, SectionDefinitionId = selected.Id, DisplayName = selected.Name
        }, definitions));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSection(TemplateSectionComposerViewModel model)
    {
        var template = await db.PageTemplates.FirstOrDefaultAsync(x => x.Id == model.TemplateId && x.IsEnabled);
        var definition = await db.SectionDefinitions.FirstOrDefaultAsync(x => x.Id == model.SectionDefinitionId && x.IsEnabled);
        if (template is null || definition is null) return NotFound();
        ValidateLayout(definition, model);
        if (!ModelState.IsValid) return View(await PrepareComposerModelAsync(model));
        var nextOrder = (await db.TemplateSections.Where(x => x.TemplateId == template.Id).MaxAsync(x => (int?)x.SortOrder) ?? 0) + 10;
        var keyPrefix = definition.Key[..Math.Min(definition.Key.Length, 70)];
        var sectionKey = $"{keyPrefix}-{Guid.NewGuid():N}"[..(keyPrefix.Length + 9)];
        db.TemplateSections.Add(new TemplateSection
        {
            TemplateId = template.Id, SectionDefinitionId = definition.Id, SectionKey = sectionKey,
            DisplayName = model.DisplayName.Trim(), SortOrder = nextOrder, IsEnabled = model.IsEnabled,
            IsEnabledByDefault = true, SettingsJson = SerializeSettings(model.Layout)
        });
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã thêm section {model.DisplayName}.";
        return RedirectToAction(nameof(Sections), new { id = template.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditSection(int id)
    {
        var section = await db.TemplateSections.AsNoTracking().Include(x => x.SectionDefinition).FirstOrDefaultAsync(x => x.Id == id);
        if (section is null) return NotFound();
        return View(await PrepareComposerModelAsync(new TemplateSectionComposerViewModel
        {
            Id = section.Id, TemplateId = section.TemplateId, SectionDefinitionId = section.SectionDefinitionId,
            DisplayName = section.DisplayName, IsEnabled = section.IsEnabled,
            Layout = sectionSchemas.ResolveSetting(section.SectionDefinition.SchemaJson, section.SettingsJson, "layout")
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSection(TemplateSectionComposerViewModel model)
    {
        var section = await db.TemplateSections.Include(x => x.SectionDefinition).FirstOrDefaultAsync(x => x.Id == model.Id && x.TemplateId == model.TemplateId);
        if (section is null) return NotFound();
        ValidateLayout(section.SectionDefinition, model);
        if (section.IsRequired && !model.IsEnabled) ModelState.AddModelError(nameof(model.IsEnabled), "Section bắt buộc không thể bị ẩn.");
        if (!ModelState.IsValid) return View(await PrepareComposerModelAsync(model));
        section.DisplayName = model.DisplayName.Trim(); section.IsEnabled = model.IsEnabled;
        section.SettingsJson = SerializeSettings(model.Layout);
        await db.SaveChangesAsync();
        TempData["Message"] = $"Đã cập nhật section {section.DisplayName}.";
        return RedirectToAction(nameof(Sections), new { id = section.TemplateId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveSection(int id, int direction)
    {
        var current = await db.TemplateSections.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        var ordered = await db.TemplateSections.Where(x => x.TemplateId == current.TemplateId).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        var index = ordered.FindIndex(x => x.Id == id); var target = index + Math.Sign(direction);
        if (index >= 0 && target >= 0 && target < ordered.Count)
        {
            (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
            for (var i = 0; i < ordered.Count; i++) ordered[i].SortOrder = (i + 1) * 10;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Sections), new { id = current.TemplateId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSection(int id)
    {
        var section = await db.TemplateSections.FirstOrDefaultAsync(x => x.Id == id);
        if (section is null) return NotFound();
        if (section.IsRequired) { TempData["Message"] = "Không thể gỡ section bắt buộc."; return RedirectToAction(nameof(Sections), new { id = section.TemplateId }); }
        var templateId = section.TemplateId; db.TemplateSections.Remove(section); await db.SaveChangesAsync();
        TempData["Message"] = "Đã gỡ section khỏi template. Nội dung và hình ảnh vẫn được giữ lại.";
        return RedirectToAction(nameof(Sections), new { id = templateId });
    }

    private async Task<TemplateSectionComposerViewModel> PrepareComposerModelAsync(TemplateSectionComposerViewModel model, IReadOnlyList<SectionDefinition>? definitions = null)
    {
        model.Definitions = definitions ?? await db.SectionDefinitions.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.Name).ToListAsync();
        var definition = model.Definitions.FirstOrDefault(x => x.Id == model.SectionDefinitionId);
        if (definition is not null)
        {
            model.DefinitionName = definition.Name;
            var layout = sectionSchemas.GetSetting(definition.SchemaJson, "layout");
            model.LayoutOptions = (IReadOnlyList<SectionSettingOption>?)layout?.Options ?? Array.Empty<SectionSettingOption>();
            model.Layout ??= layout?.Default;
        }
        return model;
    }

    private void ValidateLayout(SectionDefinition definition, TemplateSectionComposerViewModel model)
    {
        var layout = sectionSchemas.GetSetting(definition.SchemaJson, "layout");
        if (layout is null) { model.Layout = null; return; }
        model.Layout ??= layout.Default;
        if (!layout.Options.Any(x => x.Value == model.Layout)) ModelState.AddModelError(nameof(model.Layout), "Kiểu hiển thị không hợp lệ.");
    }

    private static string SerializeSettings(string? layout) => string.IsNullOrWhiteSpace(layout) ? "{}" : JsonSerializer.Serialize(new { layout });
}
