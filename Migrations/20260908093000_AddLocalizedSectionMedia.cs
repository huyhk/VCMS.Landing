using LandingCms.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260908093000_AddLocalizedSectionMedia")]
public partial class AddLocalizedSectionMedia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "__temp_SectionItemTranslations",
            columns: table => new
            {
                SectionItemId = table.Column<long>(type: "INTEGER", nullable: false),
                LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                ContentJson = table.Column<string>(type: "TEXT", nullable: false),
                MediaAssetId = table.Column<long>(type: "INTEGER", nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SectionItemTranslations", x => new { x.SectionItemId, x.LanguageCode });
                table.ForeignKey("FK_SectionItemTranslations_ContentLanguages_LanguageCode", x => x.LanguageCode, "ContentLanguages", "Code", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SectionItemTranslations_MediaAssets_MediaAssetId", x => x.MediaAssetId, "MediaAssets", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SectionItemTranslations_SectionItems_SectionItemId", x => x.SectionItemId, "SectionItems", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.Sql("INSERT INTO \"__temp_SectionItemTranslations\" (\"SectionItemId\", \"LanguageCode\", \"ContentJson\", \"UpdatedAtUtc\", \"UpdatedById\") SELECT \"SectionItemId\", \"LanguageCode\", \"ContentJson\", \"UpdatedAtUtc\", \"UpdatedById\" FROM \"SectionItemTranslations\";");
        migrationBuilder.DropTable(name: "SectionItemTranslations");
        migrationBuilder.RenameTable(name: "__temp_SectionItemTranslations", newName: "SectionItemTranslations");
        migrationBuilder.CreateIndex(name: "IX_SectionItemTranslations_LanguageCode", table: "SectionItemTranslations", column: "LanguageCode");
        migrationBuilder.CreateIndex(name: "IX_SectionItemTranslations_MediaAssetId", table: "SectionItemTranslations", column: "MediaAssetId");

        migrationBuilder.CreateTable(
            name: "__temp_SectionMedia",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                SectionKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                MediaAssetId = table.Column<long>(type: "INTEGER", nullable: false),
                Role = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                FocalPointX = table.Column<double>(type: "REAL", nullable: false),
                FocalPointY = table.Column<double>(type: "REAL", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SectionMedia", x => x.Id);
                table.ForeignKey("FK_SectionMedia_ContentLanguages_LanguageCode", x => x.LanguageCode, "ContentLanguages", "Code", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SectionMedia_MediaAssets_MediaAssetId", x => x.MediaAssetId, "MediaAssets", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.Sql("INSERT INTO \"__temp_SectionMedia\" (\"Id\", \"SectionKey\", \"MediaAssetId\", \"Role\", \"SortOrder\", \"IsEnabled\", \"FocalPointX\", \"FocalPointY\", \"CreatedAtUtc\") SELECT \"Id\", \"SectionKey\", \"MediaAssetId\", \"Role\", \"SortOrder\", \"IsEnabled\", \"FocalPointX\", \"FocalPointY\", \"CreatedAtUtc\" FROM \"SectionMedia\";");
        migrationBuilder.DropTable(name: "SectionMedia");
        migrationBuilder.RenameTable(name: "__temp_SectionMedia", newName: "SectionMedia");
        migrationBuilder.CreateIndex(name: "IX_SectionMedia_LanguageCode", table: "SectionMedia", column: "LanguageCode");
        migrationBuilder.CreateIndex(name: "IX_SectionMedia_MediaAssetId", table: "SectionMedia", column: "MediaAssetId");
        migrationBuilder.CreateIndex(name: "IX_SectionMedia_SectionKey_LanguageCode_Role_SortOrder", table: "SectionMedia", columns: new[] { "SectionKey", "LanguageCode", "Role", "SortOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "__temp_SectionItemTranslations",
            columns: table => new
            {
                SectionItemId = table.Column<long>(type: "INTEGER", nullable: false),
                LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                ContentJson = table.Column<string>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SectionItemTranslations", x => new { x.SectionItemId, x.LanguageCode });
                table.ForeignKey("FK_SectionItemTranslations_ContentLanguages_LanguageCode", x => x.LanguageCode, "ContentLanguages", "Code", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SectionItemTranslations_SectionItems_SectionItemId", x => x.SectionItemId, "SectionItems", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.Sql("INSERT INTO \"__temp_SectionItemTranslations\" (\"SectionItemId\", \"LanguageCode\", \"ContentJson\", \"UpdatedAtUtc\", \"UpdatedById\") SELECT \"SectionItemId\", \"LanguageCode\", \"ContentJson\", \"UpdatedAtUtc\", \"UpdatedById\" FROM \"SectionItemTranslations\";");
        migrationBuilder.DropTable(name: "SectionItemTranslations");
        migrationBuilder.RenameTable(name: "__temp_SectionItemTranslations", newName: "SectionItemTranslations");
        migrationBuilder.CreateIndex(name: "IX_SectionItemTranslations_LanguageCode", table: "SectionItemTranslations", column: "LanguageCode");

        migrationBuilder.CreateTable(
            name: "__temp_SectionMedia",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                SectionKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                MediaAssetId = table.Column<long>(type: "INTEGER", nullable: false),
                Role = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                FocalPointX = table.Column<double>(type: "REAL", nullable: false),
                FocalPointY = table.Column<double>(type: "REAL", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SectionMedia", x => x.Id);
                table.ForeignKey("FK_SectionMedia_MediaAssets_MediaAssetId", x => x.MediaAssetId, "MediaAssets", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.Sql("INSERT INTO \"__temp_SectionMedia\" (\"Id\", \"SectionKey\", \"MediaAssetId\", \"Role\", \"SortOrder\", \"IsEnabled\", \"FocalPointX\", \"FocalPointY\", \"CreatedAtUtc\") SELECT \"Id\", \"SectionKey\", \"MediaAssetId\", \"Role\", \"SortOrder\", \"IsEnabled\", \"FocalPointX\", \"FocalPointY\", \"CreatedAtUtc\" FROM \"SectionMedia\";");
        migrationBuilder.DropTable(name: "SectionMedia");
        migrationBuilder.RenameTable(name: "__temp_SectionMedia", newName: "SectionMedia");
        migrationBuilder.CreateIndex(name: "IX_SectionMedia_MediaAssetId", table: "SectionMedia", column: "MediaAssetId");
        migrationBuilder.CreateIndex(name: "IX_SectionMedia_SectionKey_Role_SortOrder", table: "SectionMedia", columns: new[] { "SectionKey", "Role", "SortOrder" });
    }
}
