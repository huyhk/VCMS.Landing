using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class PageSection
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string SectionKey { get; set; } = "";
    [Required, StringLength(150)] public string DisplayName { get; set; } = "";
    public int SectionDefinitionId { get; set; }
    public SectionDefinition SectionDefinition { get; set; } = null!;
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<TemplateSection> TemplateSections { get; set; } = new List<TemplateSection>();
}
