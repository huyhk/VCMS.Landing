using LandingCms.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCMS.Landing.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260908090000_AddPopupCampaigns")]
public partial class AddPopupCampaigns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PopupCampaigns",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                InternalName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                StartAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                EndAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                TriggerDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                Frequency = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                IsDismissible = table.Column<bool>(type: "INTEGER", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PopupCampaigns", x => x.Id));

        migrationBuilder.CreateTable(
            name: "PopupCampaignTranslations",
            columns: table => new
            {
                PopupCampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                ContentHtml = table.Column<string>(type: "TEXT", nullable: true),
                ButtonText = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                ButtonUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                DesktopMediaId = table.Column<long>(type: "INTEGER", nullable: true),
                MobileMediaId = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PopupCampaignTranslations", x => new { x.PopupCampaignId, x.LanguageCode });
                table.ForeignKey("FK_PopupCampaignTranslations_ContentLanguages_LanguageCode", x => x.LanguageCode, "ContentLanguages", "Code", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PopupCampaignTranslations_MediaAssets_DesktopMediaId", x => x.DesktopMediaId, "MediaAssets", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PopupCampaignTranslations_MediaAssets_MobileMediaId", x => x.MobileMediaId, "MediaAssets", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PopupCampaignTranslations_PopupCampaigns_PopupCampaignId", x => x.PopupCampaignId, "PopupCampaigns", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_PopupCampaigns_IsEnabled_Priority_StartAtUtc_EndAtUtc", "PopupCampaigns", new[] { "IsEnabled", "Priority", "StartAtUtc", "EndAtUtc" });
        migrationBuilder.CreateIndex("IX_PopupCampaignTranslations_DesktopMediaId", "PopupCampaignTranslations", "DesktopMediaId");
        migrationBuilder.CreateIndex("IX_PopupCampaignTranslations_LanguageCode", "PopupCampaignTranslations", "LanguageCode");
        migrationBuilder.CreateIndex("IX_PopupCampaignTranslations_MobileMediaId", "PopupCampaignTranslations", "MobileMediaId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PopupCampaignTranslations");
        migrationBuilder.DropTable("PopupCampaigns");
    }
}
