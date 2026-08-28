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
    public string ContentEditor { get; set; } = "textarea";
    public string? ContentHtmlPolicy { get; set; }
    public IReadOnlySet<string> AllowedHtmlTags { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    [StringLength(100)] public string? Eyebrow { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(300)] public string? Subtitle { get; set; }
    [StringLength(20000)] public string? Content { get; set; }
    [StringLength(500)] public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public List<IFormFile> BackgroundFiles { get; set; } = new();
    public IReadOnlyList<SectionMedia> Backgrounds { get; set; } = Array.Empty<SectionMedia>();
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
