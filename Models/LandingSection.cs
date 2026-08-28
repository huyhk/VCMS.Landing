using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LandingCms.Models;

public class LandingSection
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string SectionKey { get; set; } = "section";
    [Required, StringLength(30)] public string SectionType { get; set; } = "Content";
    [StringLength(100)] public string? Eyebrow { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(300)] public string? Subtitle { get; set; }
    [StringLength(4000)] public string? Content { get; set; }
    [NotMapped] public bool ContentIsHtml { get; set; }
    [StringLength(200)] public string? ImageUrl { get; set; }
    [StringLength(80)] public string? PrimaryButtonText { get; set; }
    [StringLength(300)] public string? PrimaryButtonUrl { get; set; }
    [StringLength(80)] public string? SecondaryButtonText { get; set; }
    [StringLength(300)] public string? SecondaryButtonUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
