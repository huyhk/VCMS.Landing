namespace LandingCms.Models;

public class TemplateSetting
{
    public int TemplateId { get; set; }
    public PageTemplate Template { get; set; } = null!;
    public int SettingDefinitionId { get; set; }
    public SettingDefinition SettingDefinition { get; set; } = null!;
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? OverrideLabel { get; set; }
    public string? OverrideDefaultValue { get; set; }
}
