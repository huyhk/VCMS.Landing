using System.ComponentModel.DataAnnotations;
using LandingCms.Models;

namespace LandingCms.ViewModels;

public class SiteSettingsEditViewModel
{
    public int Id { get; set; }
    public string LanguageCode { get; set; } = "vi";
    public IReadOnlyList<ContentLanguage> Languages { get; set; } = Array.Empty<ContentLanguage>();
    public bool IsDefaultLanguage { get; set; } = true;
    public bool HasTranslation { get; set; }
    [Required, StringLength(100)] public string SiteName { get; set; } = "";
    [StringLength(200)] public string? CompanyName { get; set; }
    [StringLength(200)] public string? LogoText { get; set; }
    [StringLength(200)] public string? SeoTitle { get; set; }
    [StringLength(500)] public string? SeoDescription { get; set; }
    [StringLength(500)] public string? SeoKeywords { get; set; }
    [StringLength(100)] public string? Phone { get; set; }
    [StringLength(150)] public string? Email { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    [StringLength(500)] public string? FooterText { get; set; }
}
