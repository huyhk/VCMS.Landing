using LandingCms.Models;

namespace LandingCms.ViewModels;
public record HomeViewModel(
    SiteSetting Settings,
    IReadOnlyList<LandingSection> Sections,
    IReadOnlyDictionary<string, string> ExtendedSettings,
    IReadOnlyDictionary<string, MediaAsset> BrandingMedia,
    IReadOnlyDictionary<string, IReadOnlyList<SectionMedia>> SectionMedia);
