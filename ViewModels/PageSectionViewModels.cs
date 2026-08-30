using System.ComponentModel.DataAnnotations;
using LandingCms.Models;

namespace LandingCms.ViewModels;

public record PageSectionListItemViewModel(PageSection Section, int TemplateCount, bool HasContent);

public class PageSectionCreateViewModel
{
    [Required] public int SectionDefinitionId { get; set; }
    [Required, StringLength(150)] public string DisplayName { get; set; } = "";
    public IReadOnlyList<SectionDefinition> Definitions { get; set; } = Array.Empty<SectionDefinition>();
}
