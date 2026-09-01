using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SectionContentTranslation
{
    public int SectionContentId { get; set; }
    public SectionContent SectionContent { get; set; } = null!;
    [StringLength(10)] public string LanguageCode { get; set; } = "";
    public ContentLanguage Language { get; set; } = null!;
    public string ContentJson { get; set; } = "{}";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? UpdatedById { get; set; }
}
