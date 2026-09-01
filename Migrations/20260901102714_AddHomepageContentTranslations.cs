using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations
{
    /// <inheritdoc />
    public partial class AddHomepageContentTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentLanguages",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentLanguages", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SectionContentTranslations",
                columns: table => new
                {
                    SectionContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ContentJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionContentTranslations", x => new { x.SectionContentId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_SectionContentTranslations_ContentLanguages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalTable: "ContentLanguages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SectionContentTranslations_SectionContents_SectionContentId",
                        column: x => x.SectionContentId,
                        principalTable: "SectionContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionItemTranslations",
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
                    table.ForeignKey(
                        name: "FK_SectionItemTranslations_ContentLanguages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalTable: "ContentLanguages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SectionItemTranslations_SectionItems_SectionItemId",
                        column: x => x.SectionItemId,
                        principalTable: "SectionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentLanguages_IsDefault",
                table: "ContentLanguages",
                column: "IsDefault",
                unique: true,
                filter: "\"IsDefault\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SectionContentTranslations_LanguageCode",
                table: "SectionContentTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_SectionItemTranslations_LanguageCode",
                table: "SectionItemTranslations",
                column: "LanguageCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectionContentTranslations");

            migrationBuilder.DropTable(
                name: "SectionItemTranslations");

            migrationBuilder.DropTable(
                name: "ContentLanguages");
        }
    }
}
