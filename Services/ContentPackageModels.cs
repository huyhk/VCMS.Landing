using LandingCms.Models;

namespace LandingCms.Services;

public sealed class ContentPackageManifest
{
    public const int CurrentSchemaVersion = 1;
    public string Format { get; set; } = "VCMS.ContentPackage";
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    public string? SiteName { get; set; }
    public string? ActiveTemplateKey { get; set; }
    public string? ActiveThemeKey { get; set; }
    public int SectionCount { get; set; }
    public int MediaCount { get; set; }
    public long MediaBytes { get; set; }
}

public sealed class ContentPackageData
{
    public List<LandingSection> LandingSections { get; set; } = [];
    public List<SiteSetting> SiteSettings { get; set; } = [];
    public List<SiteSettingTranslation> SiteSettingTranslations { get; set; } = [];
    public List<PageTemplate> PageTemplates { get; set; } = [];
    public List<SectionDefinition> SectionDefinitions { get; set; } = [];
    public List<PageSection> PageSections { get; set; } = [];
    public List<TemplateSection> TemplateSections { get; set; } = [];
    public List<TemplateSectionTranslation> TemplateSectionTranslations { get; set; } = [];
    public List<SectionContent> SectionContents { get; set; } = [];
    public List<SectionContentTranslation> SectionContentTranslations { get; set; } = [];
    public List<SectionItem> SectionItems { get; set; } = [];
    public List<SectionItemTranslation> SectionItemTranslations { get; set; } = [];
    public List<SiteTemplateSetting> SiteTemplateSettings { get; set; } = [];
    public List<ThemeDefinition> ThemeDefinitions { get; set; } = [];
    public List<SiteThemeSetting> SiteThemeSettings { get; set; } = [];
    public List<SettingDefinition> SettingDefinitions { get; set; } = [];
    public List<SettingValue> SettingValues { get; set; } = [];
    public List<TemplateSetting> TemplateSettings { get; set; } = [];
    public List<MediaAsset> MediaAssets { get; set; } = [];
    public List<SectionMedia> SectionMedia { get; set; } = [];
    public List<ContentLanguage> ContentLanguages { get; set; } = [];
}

public sealed record ContentPackageInspection(
    string Token, ContentPackageManifest Manifest, int LanguageCount, int TemplateCount,
    int ThemeCount, int SectionItemCount, long PackageBytes, IReadOnlyList<string> Warnings);
