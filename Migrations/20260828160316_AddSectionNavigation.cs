using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NavigationLabel",
                table: "TemplateSections",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInNavigation",
                table: "TemplateSections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
            migrationBuilder.Sql("""
                UPDATE TemplateSections
                SET ShowInNavigation = 1,
                    NavigationLabel = CASE SectionKey
                        WHEN 'services' THEN 'Dịch vụ'
                        WHEN 'about' THEN 'Về chúng tôi'
                        WHEN 'contact' THEN 'Liên hệ'
                    END
                WHERE SectionKey IN ('services', 'about', 'contact');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NavigationLabel",
                table: "TemplateSections");

            migrationBuilder.DropColumn(
                name: "ShowInNavigation",
                table: "TemplateSections");
        }
    }
}
