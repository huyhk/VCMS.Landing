using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SectionItem
{
    public long Id { get; set; }
    [Required, StringLength(80)] public string SectionKey { get; set; } = "";
    public string ContentJson { get; set; } = "{}";
    public long? MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? UpdatedById { get; set; }
    public ICollection<SectionItemTranslation> Translations { get; set; } = new List<SectionItemTranslation>();
}
