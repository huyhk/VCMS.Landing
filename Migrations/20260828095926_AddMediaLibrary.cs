using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    RelativeUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    UploadedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SectionMedia",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                    table.ForeignKey(
                        name: "FK_SectionMedia_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_RelativeUrl",
                table: "MediaAssets",
                column: "RelativeUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionMedia_MediaAssetId",
                table: "SectionMedia",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionMedia_SectionKey_Role_SortOrder",
                table: "SectionMedia",
                columns: new[] { "SectionKey", "Role", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectionMedia");

            migrationBuilder.DropTable(
                name: "MediaAssets");
        }
    }
}
