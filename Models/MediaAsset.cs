using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class MediaAsset
{
    public long Id { get; set; }
    [Required, StringLength(255)] public string OriginalFileName { get; set; } = "";
    [Required, StringLength(255)] public string StoredFileName { get; set; } = "";
    [Required, StringLength(500)] public string RelativeUrl { get; set; } = "";
    [Required, StringLength(100)] public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    [StringLength(450)] public string? UploadedById { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public ICollection<SectionMedia> SectionUsages { get; set; } = new List<SectionMedia>();
    public ICollection<SectionItem> SectionItemUsages { get; set; } = new List<SectionItem>();
}
