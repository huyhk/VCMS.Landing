using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SectionDefinition
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Key { get; set; } = "";
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, StringLength(80)] public string SectionType { get; set; } = "";
    public string SchemaJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
    public ICollection<TemplateSection> TemplateSections { get; set; } = new List<TemplateSection>();
}
