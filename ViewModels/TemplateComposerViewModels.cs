using System.ComponentModel.DataAnnotations;
using LandingCms.Models;
using LandingCms.Services;

namespace LandingCms.ViewModels;

public record TemplateComposerViewModel(PageTemplate Template, IReadOnlyList<TemplateSection> Sections, bool IsActive);

public class TemplateSectionComposerViewModel
{
    public int? Id { get; set; }
    public int TemplateId { get; set; }
    [Required] public int SectionDefinitionId { get; set; }
    [Required, StringLength(150)] public string DisplayName { get; set; } = "";
    public string? Layout { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool ShowInNavigation { get; set; }
    [StringLength(50)] public string? NavigationLabel { get; set; }
    public bool NavigationAllowed { get; set; }
    public string DefinitionName { get; set; } = "";
    public IReadOnlyList<SectionDefinition> Definitions { get; set; } = Array.Empty<SectionDefinition>();
    public IReadOnlyList<SectionSettingOption> LayoutOptions { get; set; } = Array.Empty<SectionSettingOption>();
}
