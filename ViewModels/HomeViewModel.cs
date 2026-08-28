using LandingCms.Models;

namespace LandingCms.ViewModels;
public record NavigationItem(string SectionKey, string Label);
public record SectionRenderViewModel(LandingSection Section, HomeViewModel Page);
public record HomeViewModel(
    SiteSetting Settings,
    IReadOnlyList<LandingSection> Sections,
    IReadOnlyList<NavigationItem> NavigationItems,
    IReadOnlyDictionary<string, string> ExtendedSettings,
    IReadOnlyDictionary<string, MediaAsset> BrandingMedia,
    IReadOnlyDictionary<string, IReadOnlyList<SectionMedia>> SectionMedia);
