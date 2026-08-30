using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class ThemeDefinition
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Key { get; set; } = "";
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    public string TokensJson { get; set; } = "{}";
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    [StringLength(30)] public string Version { get; set; } = "1.0";
    [Required, StringLength(20)] public string Source { get; set; } = "Custom";
    public bool IsReadOnly { get; set; }
    public int? BaseThemeId { get; set; }
    public ThemeDefinition? BaseTheme { get; set; }
    public ICollection<ThemeDefinition> DerivedThemes { get; set; } = new List<ThemeDefinition>();
    [StringLength(256)] public string? CreatedBy { get; set; }
    [StringLength(256)] public string? UpdatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
