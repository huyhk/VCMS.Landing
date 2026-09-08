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
        migrationBuilder.AddColumn<string>(name: "LanguageCode", table: "SectionMedia", type: "TEXT", maxLength: 10, nullable: true);
        migrationBuilder.AddColumn<long>(name: "MediaAssetId", table: "SectionItemTranslations", type: "INTEGER", nullable: true);
        migrationBuilder.DropIndex(name: "IX_SectionMedia_SectionKey_Role_SortOrder", table: "SectionMedia");
        migrationBuilder.CreateIndex(name: "IX_SectionMedia_LanguageCode", table: "SectionMedia", column: "LanguageCode");
        migrationBuilder.CreateIndex(name: "IX_SectionMedia_SectionKey_LanguageCode_Role_SortOrder", table: "SectionMedia", columns: new[] { "SectionKey", "LanguageCode", "Role", "SortOrder" });
        migrationBuilder.CreateIndex(name: "IX_SectionItemTranslations_MediaAssetId", table: "SectionItemTranslations", column: "MediaAssetId");
        migrationBuilder.AddForeignKey(name: "FK_SectionMedia_ContentLanguages_LanguageCode", table: "SectionMedia", column: "LanguageCode", principalTable: "ContentLanguages", principalColumn: "Code", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_SectionItemTranslations_MediaAssets_MediaAssetId", table: "SectionItemTranslations", column: "MediaAssetId", principalTable: "MediaAssets", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_SectionMedia_ContentLanguages_LanguageCode", table: "SectionMedia");
        migrationBuilder.DropForeignKey(name: "FK_SectionItemTranslations_MediaAssets_MediaAssetId", table: "SectionItemTranslations");
        migrationBuilder.DropIndex(name: "IX_SectionMedia_LanguageCode", table: "SectionMedia");
        migrationBuilder.DropIndex(name: "IX_SectionMedia_SectionKey_LanguageCode_Role_SortOrder", table: "SectionMedia");
        migrationBuilder.DropIndex(name: "IX_SectionItemTranslations_MediaAssetId", table: "SectionItemTranslations");
        migrationBuilder.DropColumn(name: "LanguageCode", table: "SectionMedia");
        migrationBuilder.DropColumn(name: "MediaAssetId", table: "SectionItemTranslations");
        migrationBuilder.CreateIndex(name: "IX_SectionMedia_SectionKey_Role_SortOrder", table: "SectionMedia", columns: new[] { "SectionKey", "Role", "SortOrder" });
    }
}
