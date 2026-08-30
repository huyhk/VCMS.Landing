using LandingCms.Models;

namespace LandingCms.ViewModels;

public sealed record ThemeListItemViewModel(
    ThemeDefinition Theme,
    bool IsActive,
    IReadOnlyDictionary<string, string> Tokens);
