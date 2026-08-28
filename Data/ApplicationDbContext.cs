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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<PageTemplate>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<SectionDefinition>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<TemplateSection>().HasIndex(x => new { x.TemplateId, x.SectionKey }).IsUnique();
        builder.Entity<SectionContent>().HasIndex(x => x.SectionKey).IsUnique();
        builder.Entity<SiteTemplateSetting>()
            .HasOne(x => x.ActiveTemplate).WithMany().HasForeignKey(x => x.ActiveTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SiteTemplateSetting>()
            .HasOne(x => x.DraftTemplate).WithMany().HasForeignKey(x => x.DraftTemplateId).OnDelete(DeleteBehavior.Restrict);
    }
}
