using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class TemplateSectionTranslation
{
    public int TemplateSectionId { get; set; }
    public TemplateSection TemplateSection { get; set; } = null!;
    [StringLength(10)] public string LanguageCode { get; set; } = "";
    public ContentLanguage Language { get; set; } = null!;
    [StringLength(50)] public string? NavigationLabel { get; set; }
}
