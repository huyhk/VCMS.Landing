using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class TemplateSection
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public PageTemplate Template { get; set; } = null!;
    public int SectionDefinitionId { get; set; }
    public SectionDefinition SectionDefinition { get; set; } = null!;
    [Required, StringLength(80)] public string SectionKey { get; set; } = "";
    [Required, StringLength(150)] public string DisplayName { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEnabledByDefault { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    [StringLength(300)] public string? ViewPath { get; set; }
    public string SettingsJson { get; set; } = "{}";
}
