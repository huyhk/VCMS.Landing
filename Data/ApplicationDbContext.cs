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
    public DbSet<TemplateSection> TemplateSections => Set<TemplateSection>();
    public DbSet<SectionContent> SectionContents => Set<SectionContent>();
    public DbSet<SiteTemplateSetting> SiteTemplateSettings => Set<SiteTemplateSetting>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();
    public DbSet<SettingDefinition> SettingDefinitions => Set<SettingDefinition>();
    public DbSet<SettingValue> SettingValues => Set<SettingValue>();
    public DbSet<TemplateSetting> TemplateSettings => Set<TemplateSetting>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<SectionMedia> SectionMedia => Set<SectionMedia>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<PageTemplate>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<SectionDefinition>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<TemplateSection>().HasIndex(x => new { x.TemplateId, x.SectionKey }).IsUnique();
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
        builder.Entity<SiteTemplateSetting>()
            .HasOne(x => x.ActiveTemplate).WithMany().HasForeignKey(x => x.ActiveTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SiteTemplateSetting>()
            .HasOne(x => x.DraftTemplate).WithMany().HasForeignKey(x => x.DraftTemplateId).OnDelete(DeleteBehavior.Restrict);
    }
}
