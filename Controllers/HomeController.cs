using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LandingCms.Controllers;
public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync();
        var templateSetting = await db.SiteTemplateSettings.AsNoTracking()
            .Include(x => x.ActiveTemplate).FirstAsync();
        var slots = await db.TemplateSections.AsNoTracking()
            .Include(x => x.SectionDefinition)
            .Where(x => x.TemplateId == templateSetting.ActiveTemplateId)
            .OrderBy(x => x.SortOrder).ToListAsync();
        var keys = slots.Select(x => x.SectionKey).ToArray();
        var contents = await db.SectionContents.AsNoTracking()
            .Where(x => keys.Contains(x.SectionKey) && x.IsPublished)
            .ToDictionaryAsync(x => x.SectionKey);
        var sections = new List<LandingSection>();
        foreach (var slot in slots)
        {
            if (!contents.TryGetValue(slot.SectionKey, out var content)) continue;
            var payload = JsonSerializer.Deserialize<SectionContentPayload>(content.ContentJson) ?? new();
            sections.Add(new LandingSection
            {
                SectionKey = slot.SectionKey, SectionType = slot.SectionDefinition.SectionType,
                Eyebrow = payload.Eyebrow, Title = payload.Title, Subtitle = payload.Subtitle,
                Content = payload.Content, ImageUrl = payload.ImageUrl,
                PrimaryButtonText = payload.PrimaryButtonText, PrimaryButtonUrl = payload.PrimaryButtonUrl,
                SecondaryButtonText = payload.SecondaryButtonText, SecondaryButtonUrl = payload.SecondaryButtonUrl,
                SortOrder = slot.SortOrder, IsPublished = true
            });
        }
        var viewPath = templateSetting.ActiveTemplate.ViewPath;
        if (!viewPath.StartsWith("~/Views/Templates/", StringComparison.Ordinal) ||
            !viewPath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) || viewPath.Contains(".."))
            return Problem("Template view path is invalid.");
        return View(viewPath, new HomeViewModel(settings, sections));
    }
    public IActionResult Error() => View();
}
