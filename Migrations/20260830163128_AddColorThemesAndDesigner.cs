using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations
{
    /// <inheritdoc />
    public partial class AddColorThemesAndDesigner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseThemeId",
                table: "ThemeDefinitions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ThemeDefinitions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ThemeDefinitions",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadOnly",
                table: "ThemeDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ThemeDefinitions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ThemeDefinitions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ThemeDefinitions",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThemeDefinitions_BaseThemeId",
                table: "ThemeDefinitions",
                column: "BaseThemeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ThemeDefinitions_ThemeDefinitions_BaseThemeId",
                table: "ThemeDefinitions",
                column: "BaseThemeId",
                principalTable: "ThemeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ThemeDefinitions_ThemeDefinitions_BaseThemeId",
                table: "ThemeDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ThemeDefinitions_BaseThemeId",
                table: "ThemeDefinitions");

            migrationBuilder.DropColumn(
                name: "BaseThemeId",
                table: "ThemeDefinitions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ThemeDefinitions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ThemeDefinitions");

            migrationBuilder.DropColumn(
                name: "IsReadOnly",
                table: "ThemeDefinitions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ThemeDefinitions");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ThemeDefinitions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ThemeDefinitions");
        }
    }
}
