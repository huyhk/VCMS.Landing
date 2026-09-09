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

public class PageSectionEditViewModel
{
    public int Id { get; set; }
    public string SectionKey { get; set; } = "";
    public string SectionDefinitionName { get; set; } = "";
    public string SectionType { get; set; } = "";
    [Required, StringLength(150)] public string DisplayName { get; set; } = "";
    public bool IsArchived { get; set; }
}
