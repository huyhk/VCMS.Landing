using LandingCms.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260906090000_AddTemplateChromeLayouts")]
public partial class AddTemplateChromeLayouts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FooterLayoutJson",
            table: "PageTemplates",
            type: "TEXT",
            nullable: false,
            defaultValue: "{}");

        migrationBuilder.AddColumn<string>(
            name: "HeaderLayoutJson",
            table: "PageTemplates",
            type: "TEXT",
            nullable: false,
            defaultValue: "{}");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FooterLayoutJson", table: "PageTemplates");
        migrationBuilder.DropColumn(name: "HeaderLayoutJson", table: "PageTemplates");
    }
}
