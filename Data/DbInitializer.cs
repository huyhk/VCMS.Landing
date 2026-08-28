using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LandingCms.Data;

public static class DbInitializer
{
    public const string SuperAdministrator = "SuperAdministrator";
    public const string Administrator = "Administrator";
    public const string Editor = "Editor";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        Directory.CreateDirectory(Path.Combine(environment.ContentRootPath, "App_Data"));
        await db.Database.EnsureCreatedAsync();
        await EnsureTemplateSchemaAsync(db);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { SuperAdministrator, Administrator, Editor })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        if (!await db.SiteSettings.AnyAsync())
            db.SiteSettings.Add(new SiteSetting { SiteName = "Nova Studio", LogoText = "NOVA", SeoTitle = "Nova Studio — Biến ý tưởng thành tăng trưởng", SeoDescription = "Giải pháp số tinh gọn cho doanh nghiệp hiện đại.", Phone = "0900 000 000", Email = "hello@example.com", Address = "TP. Hồ Chí Minh", FooterText = "© 2026 Nova Studio. All rights reserved." });

        if (!await db.LandingSections.AnyAsync())
        {
            db.LandingSections.AddRange(
                new LandingSection { SectionKey = "hero", SectionType = "Hero", Eyebrow = "ĐỐI TÁC TĂNG TRƯỞNG SỐ", Title = "Ý tưởng tốt xứng đáng với một trải nghiệm xuất sắc.", Subtitle = "Chúng tôi kết hợp chiến lược, thiết kế và công nghệ để tạo ra những sản phẩm số có tác động thực sự.", PrimaryButtonText = "Bắt đầu dự án", PrimaryButtonUrl = "#contact", SecondaryButtonText = "Khám phá dịch vụ", SecondaryButtonUrl = "#services", SortOrder = 10 },
                new LandingSection { SectionKey = "services", SectionType = "Cards", Eyebrow = "DỊCH VỤ", Title = "Từ chiến lược đến sản phẩm hoàn chỉnh", Subtitle = "Một đội ngũ gọn, một quy trình rõ và một mục tiêu chung: tạo ra kết quả.", Content = "Chiến lược & tư vấn|Xác định đúng vấn đề và lộ trình ưu tiên.\nThiết kế trải nghiệm|Giao diện trực quan, nhất quán và dễ sử dụng.\nPhát triển sản phẩm|Website nhanh, an toàn và dễ mở rộng.", SortOrder = 20 },
                new LandingSection { SectionKey = "about", SectionType = "Content", Eyebrow = "VỀ CHÚNG TÔI", Title = "Nhỏ gọn để linh hoạt. Đủ kinh nghiệm để đi đường dài.", Content = "Chúng tôi đồng hành sát với khách hàng từ buổi trao đổi đầu tiên đến khi sản phẩm vận hành ổn định. Mỗi quyết định đều dựa trên mục tiêu kinh doanh, không chỉ là thẩm mỹ.", ImageUrl = "https://images.unsplash.com/photo-1521737711867-e3b97375f902?auto=format&fit=crop&w=1200&q=80", SortOrder = 30 },
                new LandingSection { SectionKey = "stats", SectionType = "Stats", Title = "Kết quả được đo bằng giá trị", Content = "50+|Dự án hoàn thành\n8 năm|Kinh nghiệm\n92%|Khách hàng quay lại\n24/7|Hỗ trợ vận hành", SortOrder = 40 },
                new LandingSection { SectionKey = "contact", SectionType = "Cta", Eyebrow = "BẮT ĐẦU", Title = "Sẵn sàng biến ý tưởng thành hiện thực?", Subtitle = "Hãy kể cho chúng tôi về dự án của bạn. Buổi tư vấn đầu tiên hoàn toàn miễn phí.", PrimaryButtonText = "Liên hệ ngay", PrimaryButtonUrl = "mailto:hello@example.com", SortOrder = 50 }
            );
        }
        await db.SaveChangesAsync();
        await SeedTemplateEngineAsync(db);

        var userName = config["InitialAdmin:UserName"] ?? Environment.GetEnvironmentVariable("LANDINGCMS_ADMIN_USERNAME");
        var email = config["InitialAdmin:Email"] ?? Environment.GetEnvironmentVariable("LANDINGCMS_ADMIN_EMAIL");
        var password = config["InitialAdmin:Password"] ?? Environment.GetEnvironmentVariable("LANDINGCMS_ADMIN_PASSWORD");
        if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            if (await users.FindByNameAsync(userName) is null)
            {
                var user = new ApplicationUser { UserName = userName, Email = string.IsNullOrWhiteSpace(email) ? null : email, DisplayName = "Super Admin", EmailConfirmed = !string.IsNullOrWhiteSpace(email) };
                var result = await users.CreateAsync(user, password);
                if (result.Succeeded) await users.AddToRoleAsync(user, SuperAdministrator);
            }
        }
    }

    private static async Task EnsureTemplateSchemaAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "PageTemplates" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PageTemplates" PRIMARY KEY AUTOINCREMENT,
                "Key" TEXT NOT NULL, "Name" TEXT NOT NULL, "Description" TEXT NULL,
                "ViewPath" TEXT NOT NULL, "PreviewImageUrl" TEXT NULL, "Version" TEXT NOT NULL,
                "IsEnabled" INTEGER NOT NULL, "CreatedAtUtc" TEXT NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PageTemplates_Key" ON "PageTemplates" ("Key");
            CREATE TABLE IF NOT EXISTS "SectionDefinitions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SectionDefinitions" PRIMARY KEY AUTOINCREMENT,
                "Key" TEXT NOT NULL, "Name" TEXT NOT NULL, "SectionType" TEXT NOT NULL,
                "SchemaJson" TEXT NOT NULL, "IsEnabled" INTEGER NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SectionDefinitions_Key" ON "SectionDefinitions" ("Key");
            CREATE TABLE IF NOT EXISTS "TemplateSections" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_TemplateSections" PRIMARY KEY AUTOINCREMENT,
                "TemplateId" INTEGER NOT NULL, "SectionDefinitionId" INTEGER NOT NULL,
                "SectionKey" TEXT NOT NULL, "DisplayName" TEXT NOT NULL, "SortOrder" INTEGER NOT NULL,
                "IsRequired" INTEGER NOT NULL, "IsEnabledByDefault" INTEGER NOT NULL,
                "ViewPath" TEXT NULL, "SettingsJson" TEXT NOT NULL,
                CONSTRAINT "FK_TemplateSections_PageTemplates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES "PageTemplates" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_TemplateSections_SectionDefinitions_SectionDefinitionId" FOREIGN KEY ("SectionDefinitionId") REFERENCES "SectionDefinitions" ("Id") ON DELETE CASCADE);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TemplateSections_TemplateId_SectionKey" ON "TemplateSections" ("TemplateId", "SectionKey");
            CREATE INDEX IF NOT EXISTS "IX_TemplateSections_SectionDefinitionId" ON "TemplateSections" ("SectionDefinitionId");
            CREATE TABLE IF NOT EXISTS "SectionContents" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SectionContents" PRIMARY KEY AUTOINCREMENT,
                "SectionKey" TEXT NOT NULL, "SectionDefinitionId" INTEGER NOT NULL,
                "ContentJson" TEXT NOT NULL, "IsPublished" INTEGER NOT NULL,
                "UpdatedAtUtc" TEXT NOT NULL, "UpdatedById" TEXT NULL,
                CONSTRAINT "FK_SectionContents_SectionDefinitions_SectionDefinitionId" FOREIGN KEY ("SectionDefinitionId") REFERENCES "SectionDefinitions" ("Id") ON DELETE CASCADE);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SectionContents_SectionKey" ON "SectionContents" ("SectionKey");
            CREATE INDEX IF NOT EXISTS "IX_SectionContents_SectionDefinitionId" ON "SectionContents" ("SectionDefinitionId");
            CREATE TABLE IF NOT EXISTS "SiteTemplateSettings" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SiteTemplateSettings" PRIMARY KEY AUTOINCREMENT,
                "ActiveTemplateId" INTEGER NOT NULL, "DraftTemplateId" INTEGER NULL, "UpdatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_SiteTemplateSettings_PageTemplates_ActiveTemplateId" FOREIGN KEY ("ActiveTemplateId") REFERENCES "PageTemplates" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SiteTemplateSettings_PageTemplates_DraftTemplateId" FOREIGN KEY ("DraftTemplateId") REFERENCES "PageTemplates" ("Id") ON DELETE RESTRICT);
            CREATE INDEX IF NOT EXISTS "IX_SiteTemplateSettings_ActiveTemplateId" ON "SiteTemplateSettings" ("ActiveTemplateId");
            CREATE INDEX IF NOT EXISTS "IX_SiteTemplateSettings_DraftTemplateId" ON "SiteTemplateSettings" ("DraftTemplateId");
            """);
    }

    private static async Task SeedTemplateEngineAsync(ApplicationDbContext db)
    {
        if (!await db.SectionDefinitions.AnyAsync())
        {
            db.SectionDefinitions.AddRange(
                new SectionDefinition { Key = "hero", Name = "Hero", SectionType = "Hero", SchemaJson = DefaultSchema },
                new SectionDefinition { Key = "cards", Name = "Danh sách thẻ", SectionType = "Cards", SchemaJson = DefaultSchema },
                new SectionDefinition { Key = "content", Name = "Nội dung và hình ảnh", SectionType = "Content", SchemaJson = DefaultSchema },
                new SectionDefinition { Key = "stats", Name = "Số liệu", SectionType = "Stats", SchemaJson = DefaultSchema },
                new SectionDefinition { Key = "cta", Name = "Kêu gọi hành động", SectionType = "Cta", SchemaJson = DefaultSchema });
            await db.SaveChangesAsync();
        }

        var template = await db.PageTemplates.FirstOrDefaultAsync(x => x.Key == "corporate");
        if (template is null)
        {
            template = new PageTemplate
            {
                Key = "corporate", Name = "Corporate", Description = "Layout doanh nghiệp hiện đại mặc định.",
                ViewPath = "~/Views/Templates/Corporate/Index.cshtml", Version = "1.0", IsEnabled = true
            };
            db.PageTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        if (!await db.TemplateSections.AnyAsync(x => x.TemplateId == template.Id))
        {
            var definitions = await db.SectionDefinitions.ToDictionaryAsync(x => x.SectionType);
            var legacySections = await db.LandingSections.OrderBy(x => x.SortOrder).ToListAsync();
            foreach (var section in legacySections)
            {
                if (!definitions.TryGetValue(section.SectionType, out var definition)) continue;
                db.TemplateSections.Add(new TemplateSection
                {
                    TemplateId = template.Id, SectionDefinitionId = definition.Id, SectionKey = section.SectionKey,
                    DisplayName = section.Title, SortOrder = section.SortOrder, IsRequired = section.SectionType is "Hero" or "Cta",
                    IsEnabledByDefault = section.IsPublished
                });
                if (!await db.SectionContents.AnyAsync(x => x.SectionKey == section.SectionKey))
                {
                    var payload = new SectionContentPayload
                    {
                        Eyebrow = section.Eyebrow, Title = section.Title, Subtitle = section.Subtitle, Content = section.Content,
                        ImageUrl = section.ImageUrl, PrimaryButtonText = section.PrimaryButtonText,
                        PrimaryButtonUrl = section.PrimaryButtonUrl, SecondaryButtonText = section.SecondaryButtonText,
                        SecondaryButtonUrl = section.SecondaryButtonUrl
                    };
                    db.SectionContents.Add(new SectionContent
                    {
                        SectionKey = section.SectionKey, SectionDefinitionId = definition.Id,
                        ContentJson = JsonSerializer.Serialize(payload), IsPublished = section.IsPublished
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        var minimalTemplate = await db.PageTemplates.FirstOrDefaultAsync(x => x.Key == "minimal");
        if (minimalTemplate is null)
        {
            minimalTemplate = new PageTemplate
            {
                Key = "minimal", Name = "Minimal", Description = "Layout tối giản, tập trung vào nội dung và CTA.",
                ViewPath = "~/Views/Templates/Minimal/Index.cshtml", Version = "1.0", IsEnabled = true
            };
            db.PageTemplates.Add(minimalTemplate);
            await db.SaveChangesAsync();
        }
        if (!await db.TemplateSections.AnyAsync(x => x.TemplateId == minimalTemplate.Id))
        {
            var sourceSlots = await db.TemplateSections.AsNoTracking().Where(x => x.TemplateId == template.Id).ToListAsync();
            foreach (var slot in sourceSlots)
                db.TemplateSections.Add(new TemplateSection
                {
                    TemplateId = minimalTemplate.Id, SectionDefinitionId = slot.SectionDefinitionId,
                    SectionKey = slot.SectionKey, DisplayName = slot.DisplayName, SortOrder = slot.SortOrder,
                    IsRequired = slot.IsRequired, IsEnabledByDefault = slot.IsEnabledByDefault,
                    ViewPath = slot.ViewPath, SettingsJson = slot.SettingsJson
                });
            await db.SaveChangesAsync();
        }

        if (!await db.SiteTemplateSettings.AnyAsync())
        {
            db.SiteTemplateSettings.Add(new SiteTemplateSetting { ActiveTemplateId = template.Id });
            await db.SaveChangesAsync();
        }
    }

    private const string DefaultSchema = """
        {"fields":["eyebrow","title","subtitle","content","imageUrl","primaryButtonText","primaryButtonUrl","secondaryButtonText","secondaryButtonUrl"]}
        """;
}
