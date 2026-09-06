using LandingCms.Models;
using LandingCms.Services;

namespace LandingCms.ViewModels;
public record NavigationItem(string SectionKey, string Label);
public record SectionRenderViewModel(LandingSection Section, HomeViewModel Page);
public record ChromeRenderViewModel(HomeViewModel Page, ChromeLayout Layout, string Region);
public record HomeViewModel(
    SiteSetting Settings,
    IReadOnlyList<LandingSection> Sections,
    IReadOnlyList<NavigationItem> NavigationItems,
    string? TurnstileSiteKey,
    IReadOnlyDictionary<string, string> ExtendedSettings,
    IReadOnlyDictionary<string, MediaAsset> BrandingMedia,
    IReadOnlyDictionary<string, IReadOnlyList<SectionMedia>> SectionMedia,
    IReadOnlyDictionary<string, IReadOnlyList<SectionItem>> SectionItems,
    IReadOnlyList<ContentLanguage> Languages,
    ContentLanguage CurrentLanguage,
    ChromeLayout HeaderLayout,
    ChromeLayout FooterLayout,
    IReadOnlyDictionary<long, MediaAsset> ChromeMedia);
