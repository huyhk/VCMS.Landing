using LandingCms.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Tests;

public sealed class MigrationSmokeTests
{
    [Fact]
    public async Task All_migrations_apply_to_a_clean_sqlite_database()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);

        await db.Database.MigrateAsync();

        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, migration => migration.Contains("AddPopupCampaigns"));
        Assert.Contains(applied, migration => migration.Contains("AddLocalizedSectionMedia"));
        Assert.True(await TableExistsAsync(connection, "PopupCampaigns"));
        Assert.True(await TableExistsAsync(connection, "SectionMedia"));
    }

    [Fact]
    public async Task Database_at_104_schema_upgrades_without_rebuilding_existing_tables()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;

        await using (var oldVersion = new ApplicationDbContext(options))
        {
            await oldVersion.Database.MigrateAsync("20260906090000_AddTemplateChromeLayouts");
            oldVersion.SiteSettings.Add(new LandingCms.Models.SiteSetting { SiteName = "Existing site" });
            await oldVersion.SaveChangesAsync();
        }

        await using (var upgraded = new ApplicationDbContext(options))
        {
            await DbInitializer.MigrateDatabaseAsync(upgraded);

            Assert.True(await upgraded.SiteSettings.AnyAsync(x => x.SiteName == "Existing site"));
            Assert.Empty(await upgraded.Database.GetPendingMigrationsAsync());
        }

        Assert.True(await TableExistsAsync(connection, "PopupCampaigns"));
        Assert.True(await ColumnExistsAsync(connection, "SectionMedia", "LanguageCode"));
        Assert.True(await ColumnExistsAsync(connection, "SectionItemTranslations", "MediaAssetId"));
    }

    private static async Task<bool> ColumnExistsAsync(
        SqliteConnection connection,
        string table,
        string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        command.Parameters.AddWithValue("$name", table);
        return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
    }
}
