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
    public async Task<IActionResult> CreateSection(int templateId, int? pageSectionId)
    {
        if (!await db.PageTemplates.AnyAsync(x => x.Id == templateId && x.IsEnabled)) return NotFound();
        var sections = await LoadAvailablePageSectionsAsync(templateId);
        var selected = sections.FirstOrDefault(x => x.Id == pageSectionId) ?? sections.FirstOrDefault();
        if (selected is null)
        {
            TempData["Error"] = "Không còn section khả dụng. Hãy tạo section trong Thư viện section trước.";
            return RedirectToAction(nameof(Sections), new { id = templateId });
        }
        return View(await PrepareCreateModelAsync(new TemplateSectionComposerViewModel
        {
            TemplateId = templateId, PageSectionId = selected.Id,
            SectionDefinitionId = selected.SectionDefinitionId, DisplayName = selected.DisplayName,
            ShowInNavigation = sectionSchemas.GetNavigation(selected.SectionDefinition.SchemaJson).DefaultVisible
        }, sections));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSection(TemplateSectionComposerViewModel model)
    {
        var template = await db.PageTemplates.FirstOrDefaultAsync(x => x.Id == model.TemplateId && x.IsEnabled);
        var pageSection = await db.PageSections.Include(x => x.SectionDefinition)
            .FirstOrDefaultAsync(x => x.Id == model.PageSectionId && !x.IsArchived);
        if (template is null || pageSection is null || !pageSection.SectionDefinition.IsEnabled) return NotFound();
        if (await db.TemplateSections.AnyAsync(x => x.TemplateId == template.Id && x.PageSectionId == pageSection.Id))
            ModelState.AddModelError("", "Section này đã có trong template.");
        var definition = pageSection.SectionDefinition;
        model.SectionDefinitionId = definition.Id;
        ValidateLayout(definition, model);
        ValidateNavigation(definition, model);
        if (!ModelState.IsValid) return View(await PrepareCreateModelAsync(model));
        var nextOrder = (await db.TemplateSections.Where(x => x.TemplateId == template.Id).MaxAsync(x => (int?)x.SortOrder) ?? 0) + 10;
        db.TemplateSections.Add(new TemplateSection
        {
            TemplateId = template.Id, PageSectionId = pageSection.Id,
            SectionDefinitionId = definition.Id, SectionKey = pageSection.SectionKey,
            DisplayName = model.DisplayName.Trim(), SortOrder = nextOrder, IsEnabled = model.IsEnabled,
            IsEnabledByDefault = true, ShowInNavigation = model.ShowInNavigation,
            NavigationLabel = NormalizeNavigationLabel(model), SettingsJson = SerializeSettings(model.Layout)
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
            ShowInNavigation = section.ShowInNavigation, NavigationLabel = section.NavigationLabel,
            Layout = sectionSchemas.ResolveSetting(section.SectionDefinition.SchemaJson, section.SettingsJson, "layout")
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSection(TemplateSectionComposerViewModel model)
    {
        var section = await db.TemplateSections.Include(x => x.SectionDefinition).FirstOrDefaultAsync(x => x.Id == model.Id && x.TemplateId == model.TemplateId);
        if (section is null) return NotFound();
        ValidateLayout(section.SectionDefinition, model);
        ValidateNavigation(section.SectionDefinition, model);
        if (section.IsRequired && !model.IsEnabled) ModelState.AddModelError(nameof(model.IsEnabled), "Section bắt buộc không thể bị ẩn.");
        if (!ModelState.IsValid) return View(await PrepareComposerModelAsync(model));
        section.DisplayName = model.DisplayName.Trim(); section.IsEnabled = model.IsEnabled;
        section.ShowInNavigation = model.ShowInNavigation;
        section.NavigationLabel = NormalizeNavigationLabel(model);
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
            model.NavigationAllowed = sectionSchemas.GetNavigation(definition.SchemaJson).Allowed;
        }
        return model;
    }

    private async Task<TemplateSectionComposerViewModel> PrepareCreateModelAsync(
        TemplateSectionComposerViewModel model, IReadOnlyList<PageSection>? pageSections = null)
    {
        model.PageSections = pageSections ?? await LoadAvailablePageSectionsAsync(model.TemplateId);
        var pageSection = model.PageSections.FirstOrDefault(x => x.Id == model.PageSectionId);
        if (pageSection is not null)
        {
            model.SectionDefinitionId = pageSection.SectionDefinitionId;
            model.DefinitionName = pageSection.SectionDefinition.Name;
            var layout = sectionSchemas.GetSetting(pageSection.SectionDefinition.SchemaJson, "layout");
            model.LayoutOptions = (IReadOnlyList<SectionSettingOption>?)layout?.Options ?? Array.Empty<SectionSettingOption>();
            model.Layout ??= layout?.Default;
            model.NavigationAllowed = sectionSchemas.GetNavigation(pageSection.SectionDefinition.SchemaJson).Allowed;
        }
        return model;
    }

    private async Task<IReadOnlyList<PageSection>> LoadAvailablePageSectionsAsync(int templateId)
    {
        var assignedIds = await db.TemplateSections.Where(x => x.TemplateId == templateId && x.PageSectionId != null)
            .Select(x => x.PageSectionId!.Value).ToArrayAsync();
        return await db.PageSections.AsNoTracking().Include(x => x.SectionDefinition)
            .Where(x => !x.IsArchived && !assignedIds.Contains(x.Id))
            .OrderBy(x => x.DisplayName).ToListAsync();
    }


    private void ValidateNavigation(SectionDefinition definition, TemplateSectionComposerViewModel model)
    {
        model.NavigationAllowed = sectionSchemas.GetNavigation(definition.SchemaJson).Allowed;
        if (model.NavigationAllowed) return;
        model.ShowInNavigation = false;
        model.NavigationLabel = null;
        ModelState.Remove(nameof(model.NavigationLabel));
    }

    private static string? NormalizeNavigationLabel(TemplateSectionComposerViewModel model) =>
        model.ShowInNavigation ? (string.IsNullOrWhiteSpace(model.NavigationLabel) ? model.DisplayName.Trim() : model.NavigationLabel.Trim()) : null;

    private void ValidateLayout(SectionDefinition definition, TemplateSectionComposerViewModel model)
    {
        var layout = sectionSchemas.GetSetting(definition.SchemaJson, "layout");
        if (layout is null) { model.Layout = null; return; }
        model.Layout ??= layout.Default;
        if (!layout.Options.Any(x => x.Value == model.Layout)) ModelState.AddModelError(nameof(model.Layout), "Kiểu hiển thị không hợp lệ.");
    }

    private static string SerializeSettings(string? layout) => string.IsNullOrWhiteSpace(layout) ? "{}" : JsonSerializer.Serialize(new { layout });
}
