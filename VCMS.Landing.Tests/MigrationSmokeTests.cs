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

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        command.Parameters.AddWithValue("$name", table);
        return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
    }
}
