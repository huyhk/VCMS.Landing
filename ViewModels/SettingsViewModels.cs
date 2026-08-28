using LandingCms.Models;

namespace LandingCms.ViewModels;

public record SettingEditItemViewModel(
    int DefinitionId, string Key, string Name, string? Description, string Group,
    string ValueType, bool IsRequired, string? Value, string? DefaultValue, int SortOrder, string? MediaUrl);

public record SettingsEditViewModel(string TemplateName, IReadOnlyList<SettingEditItemViewModel> Items);
