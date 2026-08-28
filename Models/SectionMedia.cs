using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SectionMedia
{
    public long Id { get; set; }
    [Required, StringLength(80)] public string SectionKey { get; set; } = "";
    public long MediaAssetId { get; set; }
    public MediaAsset MediaAsset { get; set; } = null!;
    [Required, StringLength(40)] public string Role { get; set; } = "Background";
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public double FocalPointX { get; set; } = 50;
    public double FocalPointY { get; set; } = 50;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
