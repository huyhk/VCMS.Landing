using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SectionContent
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string SectionKey { get; set; } = "";
    public int SectionDefinitionId { get; set; }
    public SectionDefinition SectionDefinition { get; set; } = null!;
    public string ContentJson { get; set; } = "{}";
    public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? UpdatedById { get; set; }
    public ICollection<SectionContentTranslation> Translations { get; set; } = new List<SectionContentTranslation>();
}
