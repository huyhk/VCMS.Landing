using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SettingValue
{
    public int Id { get; set; }
    public int SettingDefinitionId { get; set; }
    public SettingDefinition SettingDefinition { get; set; } = null!;
    [StringLength(10000)] public string? Value { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? UpdatedById { get; set; }
}
