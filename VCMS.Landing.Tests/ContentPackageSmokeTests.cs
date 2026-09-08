using LandingCms.Data;
using LandingCms.Models;
using LandingCms.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace LandingCms.Tests;

public sealed class ContentPackageSmokeTests
{
    [Fact]
    public async Task Export_inspect_and_import_round_trip_content()
    {
        var root = Path.Combine(Path.GetTempPath(), $"vcms-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "wwwroot", "uploads"));
        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
            db.ContentLanguages.Add(new ContentLanguage
                { Code = "vi", Name = "Tiếng Việt", IsDefault = true, IsEnabled = true, SortOrder = 10 });
            db.SiteSettings.Add(new SiteSetting { SiteName = "Round trip" });
            await db.SaveChangesAsync();

            var service = new ContentPackageService(db, new TestEnvironment(root));
            var packagePath = Path.Combine(root, "roundtrip.vcms.zip");
            await using (var output = File.Create(packagePath)) await service.ExportAsync(output);
            var inspection = await service.InspectAsync(packagePath, "test");
            Assert.Equal("Round trip", inspection.Manifest.SiteName);

            db.SiteSettings.Single().SiteName = "Changed";
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            await service.ImportAsync(packagePath);

            Assert.Equal("Round trip", await db.SiteSettings.AsNoTracking().Select(x => x.SiteName).SingleAsync());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "VCMS.Landing.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
