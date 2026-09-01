using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuAndSiteSettingTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteSettingTranslations",
                columns: table => new
                {
                    SiteSettingId = table.Column<int>(type: "INTEGER", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SiteName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LogoText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SeoTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SeoDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SeoKeywords = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    FooterText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettingTranslations", x => new { x.SiteSettingId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_SiteSettingTranslations_ContentLanguages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalTable: "ContentLanguages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteSettingTranslations_SiteSettings_SiteSettingId",
                        column: x => x.SiteSettingId,
                        principalTable: "SiteSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSectionTranslations",
                columns: table => new
                {
                    TemplateSectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NavigationLabel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSectionTranslations", x => new { x.TemplateSectionId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_TemplateSectionTranslations_ContentLanguages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalTable: "ContentLanguages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSectionTranslations_TemplateSections_TemplateSectionId",
                        column: x => x.TemplateSectionId,
                        principalTable: "TemplateSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettingTranslations_LanguageCode",
                table: "SiteSettingTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionTranslations_LanguageCode",
                table: "TemplateSectionTranslations",
                column: "LanguageCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteSettingTranslations");

            migrationBuilder.DropTable(
                name: "TemplateSectionTranslations");
        }
    }
}
