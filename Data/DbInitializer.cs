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
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { SuperAdministrator, Administrator, Editor })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        if (!await db.SiteSettings.AnyAsync())
            db.SiteSettings.Add(new SiteSetting { SiteName = "VCMS Landing Studio", LogoText = "VCMS", SeoTitle = "VCMS Landing Studio — Biến ý tưởng thành tăng trưởng", SeoDescription = "Giải pháp số tinh gọn cho doanh nghiệp hiện đại.", Phone = "0900 000 000", Email = "hello@example.com", Address = "TP. Hồ Chí Minh", FooterText = "© 2026 Nova Studio. All rights reserved." });

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
                if (!result.Succeeded)
                    throw new InvalidOperationException("Không thể tạo SuperAdmin: " + string.Join("; ", result.Errors.Select(x => x.Description)));
                var roleResult = await users.AddToRoleAsync(user, SuperAdministrator);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException("Không thể gán quyền SuperAdmin: " + string.Join("; ", roleResult.Errors.Select(x => x.Description)));
            }
        }
    }

    private static async Task SeedTemplateEngineAsync(ApplicationDbContext db)
    {
        var developerSections = new[]
        {
            new DeveloperSection("hero", "Hero", "Hero", TextContentSchema),
            new DeveloperSection("cards", "Danh sách thẻ", "Cards", StructuredContentSchema),
            new DeveloperSection("content", "Nội dung và hình ảnh", "Content", RichContentSchema),
            new DeveloperSection("stats", "Số liệu", "Stats", StructuredContentSchema),
            new DeveloperSection("cta", "Kêu gọi hành động", "Cta", TextContentSchema)
        };
        var existingDefinitions = await db.SectionDefinitions.ToDictionaryAsync(x => x.Key);
        foreach (var item in developerSections)
        {
            if (!existingDefinitions.TryGetValue(item.Key, out var definition))
            {
                definition = new SectionDefinition { Key = item.Key };
                db.SectionDefinitions.Add(definition);
                existingDefinitions[item.Key] = definition;
            }
            definition.Name = item.Name;
            definition.SectionType = item.SectionType;
            definition.SchemaJson = item.SchemaJson;
            definition.IsEnabled = true;
        }
        await db.SaveChangesAsync();

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
                    IsEnabledByDefault = section.IsPublished, IsEnabled = section.IsPublished
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
                    IsEnabled = slot.IsEnabled,
                    ViewPath = slot.ViewPath, SettingsJson = slot.SettingsJson
                });
            await db.SaveChangesAsync();
        }

        var sectionLabels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["hero"] = "Hero", ["services"] = "Dịch vụ", ["about"] = "Giới thiệu",
            ["stats"] = "Số liệu", ["contact"] = "Liên hệ"
        };
        var sampleTemplateIds = new[] { template.Id, minimalTemplate.Id };
        var sampleSlots = await db.TemplateSections.Where(x => sampleTemplateIds.Contains(x.TemplateId)).ToListAsync();
        foreach (var slot in sampleSlots)
            if (sectionLabels.TryGetValue(slot.SectionKey, out var displayName))
                slot.DisplayName = displayName;
        await db.SaveChangesAsync();

        if (!await db.SiteTemplateSettings.AnyAsync())
        {
            db.SiteTemplateSettings.Add(new SiteTemplateSetting { ActiveTemplateId = template.Id });
            await db.SaveChangesAsync();
        }
        await SeedSettingsAsync(db);
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext db)
    {
        var developerSettings = new[]
        {
            new DeveloperSetting("branding.logo_primary", "Logo chính", "Nhận diện thương hiệu", "Image", "Logo dùng trên nền sáng.", 1),
            new DeveloperSetting("branding.logo_light", "Logo sáng", "Nhận diện thương hiệu", "Image", "Logo trắng/sáng dùng trên Hero hoặc nền tối.", 2),
            new DeveloperSetting("branding.favicon", "Favicon", "Nhận diện thương hiệu", "Image", "Icon hiển thị trên tab trình duyệt.", 3),
            new DeveloperSetting("social.facebook_url", "Trang Facebook", "Mạng xã hội", "Url", "URL trang Facebook của doanh nghiệp.", 10),
            new DeveloperSetting("social.zalo_url", "Tài khoản Zalo", "Mạng xã hội", "Url", "URL Zalo OA hoặc liên kết liên hệ Zalo.", 20),
            new DeveloperSetting("analytics.ga_measurement_id", "Google Analytics Measurement ID", "Phân tích", "Text", "Ví dụ: G-ABC123XYZ.", 30),
            new DeveloperSetting("analytics.gtm_container_id", "Google Tag Manager Container ID", "Phân tích", "Text", "Ví dụ: GTM-ABC1234.", 40)
        };
        var existing = await db.SettingDefinitions.ToDictionaryAsync(x => x.Key);
        foreach (var item in developerSettings)
        {
            if (!existing.TryGetValue(item.Key, out var definition))
            {
                definition = new SettingDefinition { Key = item.Key, Source = "Template", IsSystem = true };
                db.SettingDefinitions.Add(definition); existing[item.Key] = definition;
            }
            definition.Name = item.Name; definition.Group = item.Group; definition.ValueType = item.ValueType;
            definition.Description = item.Description; definition.SortOrder = item.SortOrder; definition.IsEnabled = true;
        }
        await db.SaveChangesAsync();

        var templates = await db.PageTemplates.Where(x => x.Key == "corporate" || x.Key == "minimal").ToListAsync();
        var developerKeys = developerSettings.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        var links = await db.TemplateSettings.Where(x => templates.Select(t => t.Id).Contains(x.TemplateId)).ToListAsync();
        foreach (var template in templates)
            foreach (var definition in existing.Values.Where(x => developerKeys.Contains(x.Key)))
                if (!links.Any(x => x.TemplateId == template.Id && x.SettingDefinitionId == definition.Id))
                    db.TemplateSettings.Add(new TemplateSetting { TemplateId = template.Id, SettingDefinitionId = definition.Id, SortOrder = definition.SortOrder });
        await db.SaveChangesAsync();
    }

    private const string TextContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"textarea"},"imageUrl":{"editor":"image"},"primaryButtonText":{"editor":"text"},"primaryButtonUrl":{"editor":"text"},"secondaryButtonText":{"editor":"text"},"secondaryButtonUrl":{"editor":"text"}}}
        """;

    private const string StructuredContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"structured-list"},"imageUrl":{"editor":"image"},"primaryButtonText":{"editor":"text"},"primaryButtonUrl":{"editor":"text"},"secondaryButtonText":{"editor":"text"},"secondaryButtonUrl":{"editor":"text"}}}
        """;

    private const string RichContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"html","htmlPolicy":"RichContent"},"imageUrl":{"editor":"image"},"primaryButtonText":{"editor":"text"},"primaryButtonUrl":{"editor":"text"},"secondaryButtonText":{"editor":"text"},"secondaryButtonUrl":{"editor":"text"}}}
        """;

    private sealed record DeveloperSection(string Key, string Name, string SectionType, string SchemaJson);
    private sealed record DeveloperSetting(string Key, string Name, string Group, string ValueType, string Description, int SortOrder);
}
