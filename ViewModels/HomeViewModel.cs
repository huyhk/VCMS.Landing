using LandingCms.Models;

namespace LandingCms.ViewModels;
public record HomeViewModel(SiteSetting Settings, IReadOnlyList<LandingSection> Sections);
