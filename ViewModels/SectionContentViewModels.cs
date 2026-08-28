using System.ComponentModel.DataAnnotations;
using LandingCms.Models;

namespace LandingCms.ViewModels;

public record SectionListItemViewModel(TemplateSection Slot, SectionContent? Content);

public class SectionContentEditViewModel
{
    public int TemplateSectionId { get; set; }
    public int? ContentId { get; set; }
    public string SectionKey { get; set; } = "";
    public string SectionType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    [StringLength(100)] public string? Eyebrow { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(300)] public string? Subtitle { get; set; }
    [StringLength(4000)] public string? Content { get; set; }
    [StringLength(500)] public string? ImageUrl { get; set; }
    [StringLength(80)] public string? PrimaryButtonText { get; set; }
    [StringLength(300)] public string? PrimaryButtonUrl { get; set; }
    [StringLength(80)] public string? SecondaryButtonText { get; set; }
    [StringLength(300)] public string? SecondaryButtonUrl { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class SectionContentPayload
{
    public string? Eyebrow { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public string? PrimaryButtonText { get; set; }
    public string? PrimaryButtonUrl { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonUrl { get; set; }
}
