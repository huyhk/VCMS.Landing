using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class SettingDefinition
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Key { get; set; } = "";
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    [Required, StringLength(50)] public string Group { get; set; } = "General";
    [Required, StringLength(40)] public string ValueType { get; set; } = "Text";
    [StringLength(2000)] public string? DefaultValue { get; set; }
    public string ValidationJson { get; set; } = "{}";
    [Required, StringLength(30)] public string Source { get; set; } = "Template";
    public bool IsRequired { get; set; }
    public bool IsSystem { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public SettingValue? Value { get; set; }
    public ICollection<TemplateSetting> Templates { get; set; } = new List<TemplateSetting>();
}
