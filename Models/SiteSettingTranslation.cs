using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SiteSettingTranslation
{
    public int SiteSettingId { get; set; }
    public SiteSetting SiteSetting { get; set; } = null!;
    [StringLength(10)] public string LanguageCode { get; set; } = "";
    public ContentLanguage Language { get; set; } = null!;
    [Required, StringLength(100)] public string SiteName { get; set; } = "";
    [StringLength(200)] public string? CompanyName { get; set; }
    [StringLength(200)] public string? LogoText { get; set; }
    [StringLength(200)] public string? SeoTitle { get; set; }
    [StringLength(500)] public string? SeoDescription { get; set; }
    [StringLength(500)] public string? SeoKeywords { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    [StringLength(500)] public string? FooterText { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? UpdatedById { get; set; }
}
