using LandingCms.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<LandingSection> LandingSections => Set<LandingSection>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<PageTemplate> PageTemplates => Set<PageTemplate>();
    public DbSet<SectionDefinition> SectionDefinitions => Set<SectionDefinition>();
    public DbSet<PageSection> PageSections => Set<PageSection>();
    public DbSet<TemplateSection> TemplateSections => Set<TemplateSection>();
    public DbSet<SectionContent> SectionContents => Set<SectionContent>();
    public DbSet<SiteTemplateSetting> SiteTemplateSettings => Set<SiteTemplateSetting>();
    public DbSet<ThemeDefinition> ThemeDefinitions => Set<ThemeDefinition>();
    public DbSet<SiteThemeSetting> SiteThemeSettings => Set<SiteThemeSetting>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();
    public DbSet<SettingDefinition> SettingDefinitions => Set<SettingDefinition>();
    public DbSet<SettingValue> SettingValues => Set<SettingValue>();
    public DbSet<TemplateSetting> TemplateSettings => Set<TemplateSetting>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<SectionMedia> SectionMedia => Set<SectionMedia>();
    public DbSet<SectionItem> SectionItems => Set<SectionItem>();
    public DbSet<ContentRevision> ContentRevisions => Set<ContentRevision>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<PageTemplate>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<ThemeDefinition>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<ThemeDefinition>().HasOne(x => x.BaseTheme).WithMany(x => x.DerivedThemes)
            .HasForeignKey(x => x.BaseThemeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SectionDefinition>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<PageSection>().HasIndex(x => x.SectionKey).IsUnique();
        builder.Entity<PageSection>().HasOne(x => x.SectionDefinition).WithMany(x => x.PageSections)
            .HasForeignKey(x => x.SectionDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TemplateSection>().HasIndex(x => new { x.TemplateId, x.SectionKey }).IsUnique();
        builder.Entity<TemplateSection>().HasIndex(x => new { x.TemplateId, x.PageSectionId }).IsUnique();
        builder.Entity<TemplateSection>().HasOne(x => x.PageSection).WithMany(x => x.TemplateSections)
            .HasForeignKey(x => x.PageSectionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TemplateSection>().Property(x => x.IsEnabled).HasDefaultValue(true);
        builder.Entity<TemplateSection>().Property(x => x.ShowInNavigation).HasDefaultValue(false);
        builder.Entity<SectionContent>().HasIndex(x => x.SectionKey).IsUnique();
        builder.Entity<SettingDefinition>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<SettingValue>().HasIndex(x => x.SettingDefinitionId).IsUnique();
        builder.Entity<SettingDefinition>().HasOne(x => x.Value).WithOne(x => x.SettingDefinition)
            .HasForeignKey<SettingValue>(x => x.SettingDefinitionId);
        builder.Entity<TemplateSetting>().HasKey(x => new { x.TemplateId, x.SettingDefinitionId });
        builder.Entity<TemplateSetting>().HasOne(x => x.Template).WithMany(x => x.Settings).HasForeignKey(x => x.TemplateId);
        builder.Entity<TemplateSetting>().HasOne(x => x.SettingDefinition).WithMany(x => x.Templates).HasForeignKey(x => x.SettingDefinitionId);
        builder.Entity<MediaAsset>().HasIndex(x => x.RelativeUrl).IsUnique();
        builder.Entity<SectionMedia>().HasIndex(x => new { x.SectionKey, x.Role, x.SortOrder });
        builder.Entity<SectionMedia>().HasOne(x => x.MediaAsset).WithMany(x => x.SectionUsages).HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SectionItem>().HasIndex(x => new { x.SectionKey, x.SortOrder });
        builder.Entity<SectionItem>().HasOne(x => x.MediaAsset).WithMany(x => x.SectionItemUsages)
            .HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ContentRevision>().HasIndex(x => new { x.EntityType, x.EntityKey, x.CreatedAtUtc });
        builder.Entity<SiteTemplateSetting>()
            .HasOne(x => x.ActiveTemplate).WithMany().HasForeignKey(x => x.ActiveTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SiteTemplateSetting>()
            .HasOne(x => x.DraftTemplate).WithMany().HasForeignKey(x => x.DraftTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SiteThemeSetting>()
            .HasOne(x => x.ActiveTheme).WithMany().HasForeignKey(x => x.ActiveThemeId).OnDelete(DeleteBehavior.Restrict);
    }
}
