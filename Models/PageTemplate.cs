using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class PageTemplate
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Key { get; set; } = "";
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    [Required, StringLength(300)] public string ViewPath { get; set; } = "";
    [StringLength(300)] public string? PreviewImageUrl { get; set; }
    [StringLength(30)] public string Version { get; set; } = "1.0";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<TemplateSection> Sections { get; set; } = new List<TemplateSection>();
}
