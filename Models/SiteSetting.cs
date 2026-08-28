using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SiteSetting
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string SiteName { get; set; } = "Nova Studio";
    [StringLength(200)] public string? CompanyName { get; set; }
    [StringLength(200)] public string? LogoText { get; set; }
    [StringLength(200)] public string? SeoTitle { get; set; }
    [StringLength(500)] public string? SeoDescription { get; set; }
    [StringLength(100)] public string? Phone { get; set; }
    [StringLength(150)] public string? Email { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    [StringLength(20)] public string PrimaryColor { get; set; } = "#2563eb";
    [StringLength(500)] public string? FooterText { get; set; }
}
