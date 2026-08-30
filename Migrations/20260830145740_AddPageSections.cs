using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations
{
    /// <inheritdoc />
    public partial class AddPageSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PageSectionId",
                table: "TemplateSections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PageSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SectionKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    SectionDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageSections_SectionDefinitions_SectionDefinitionId",
                        column: x => x.SectionDefinitionId,
                        principalTable: "SectionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_PageSectionId",
                table: "TemplateSections",
                column: "PageSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_TemplateId_PageSectionId",
                table: "TemplateSections",
                columns: new[] { "TemplateId", "PageSectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageSections_SectionDefinitionId",
                table: "PageSections",
                column: "SectionDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PageSections_SectionKey",
                table: "PageSections",
                column: "SectionKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateSections_PageSections_PageSectionId",
                table: "TemplateSections",
                column: "PageSectionId",
                principalTable: "PageSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateSections_PageSections_PageSectionId",
                table: "TemplateSections");

            migrationBuilder.DropTable(
                name: "PageSections");

            migrationBuilder.DropIndex(
                name: "IX_TemplateSections_PageSectionId",
                table: "TemplateSections");

            migrationBuilder.DropIndex(
                name: "IX_TemplateSections_TemplateId_PageSectionId",
                table: "TemplateSections");

            migrationBuilder.DropColumn(
                name: "PageSectionId",
                table: "TemplateSections");
        }
    }
}
