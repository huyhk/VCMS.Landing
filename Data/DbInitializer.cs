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
            db.SiteSettings.Add(new SiteSetting { SiteName = "VCMS Landing Studio", CompanyName = "Công ty TNHH Nova Studio", LogoText = "VCMS", SeoTitle = "VCMS Landing Studio — Biến ý tưởng thành tăng trưởng", SeoDescription = "Giải pháp số tinh gọn cho doanh nghiệp hiện đại.", SeoKeywords = "thiết kế website, landing page, giải pháp số", Phone = "0900 000 000", Email = "hello@example.com", Address = "TP. Hồ Chí Minh", FooterText = "© 2026 Nova Studio. All rights reserved." });

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
            new DeveloperSection("hero", "Hero", "Hero", HeroContentSchema),
            new DeveloperSection("cards", "Danh sách thẻ", "Cards", StructuredContentSchema),
            new DeveloperSection("content", "Nội dung và hình ảnh", "Content", RichContentSchema),
            new DeveloperSection("stats", "Số liệu", "Stats", StructuredContentSchema),
            new DeveloperSection("cta", "Kêu gọi hành động", "Cta", NavigableTextContentSchema),
            new DeveloperSection("gallery", "Thư viện hình ảnh", "Gallery", GalleryContentSchema),
            new DeveloperSection("faq", "Câu hỏi thường gặp", "Faq", FaqContentSchema),
            new DeveloperSection("testimonials", "Ý kiến khách hàng", "Testimonials", TestimonialsContentSchema),
            new DeveloperSection("process", "Quy trình", "Process", ProcessContentSchema),
            new DeveloperSection("media", "Video / Media", "Media", MediaContentSchema),
            new DeveloperSection("pricing", "Bảng giá", "Pricing", PricingContentSchema),
            new DeveloperSection("partners", "Đối tác / Khách hàng", "Partners", PartnersContentSchema),
            new DeveloperSection("team", "Đội ngũ", "Team", TeamContentSchema)
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
            var definitions = await db.SectionDefinitions.ToDictionaryAsync(x => x.Key);
            var legacySections = await db.LandingSections.OrderBy(x => x.SortOrder).ToListAsync();
            foreach (var section in legacySections)
            {
                var definitionKey = developerSections.FirstOrDefault(x => x.SectionType == section.SectionType)?.Key;
                if (definitionKey is null || !definitions.TryGetValue(definitionKey, out var definition)) continue;
                db.TemplateSections.Add(new TemplateSection
                {
                    TemplateId = template.Id, SectionDefinitionId = definition.Id, SectionKey = section.SectionKey,
                    DisplayName = GetDefaultDisplayName(section.SectionKey, section.Title), SortOrder = section.SortOrder, IsRequired = section.SectionType is "Hero" or "Cta",
                    IsEnabledByDefault = section.IsPublished, IsEnabled = section.IsPublished,
                    ShowInNavigation = section.SectionKey is "services" or "about" or "contact",
                    NavigationLabel = GetDefaultNavigationLabel(section.SectionKey)
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

        async Task EnsureDerivedTemplateAsync(string key, string name, string description, string viewPath)
        {
            var derivedTemplate = await db.PageTemplates.FirstOrDefaultAsync(x => x.Key == key);
            if (derivedTemplate is null)
            {
                derivedTemplate = new PageTemplate { Key = key };
                db.PageTemplates.Add(derivedTemplate);
            }
            derivedTemplate.Name = name;
            derivedTemplate.Description = description;
            derivedTemplate.ViewPath = viewPath;
            derivedTemplate.Version = "1.0";
            derivedTemplate.IsEnabled = true;
            await db.SaveChangesAsync();

            if (!await db.TemplateSections.AnyAsync(x => x.TemplateId == derivedTemplate.Id))
            {
                var sourceSlots = await db.TemplateSections.AsNoTracking().Where(x => x.TemplateId == template.Id).ToListAsync();
                foreach (var slot in sourceSlots)
                    db.TemplateSections.Add(new TemplateSection
                    {
                        TemplateId = derivedTemplate.Id, SectionDefinitionId = slot.SectionDefinitionId,
                        SectionKey = slot.SectionKey, DisplayName = slot.DisplayName, SortOrder = slot.SortOrder,
                        IsRequired = slot.IsRequired, IsEnabledByDefault = slot.IsEnabledByDefault,
                        IsEnabled = slot.IsEnabled,
                        ShowInNavigation = slot.ShowInNavigation, NavigationLabel = slot.NavigationLabel,
                        ViewPath = slot.ViewPath, SettingsJson = slot.SettingsJson
                    });
                await db.SaveChangesAsync();
            }
        }

        await EnsureDerivedTemplateAsync("minimal", "Minimal", "Bố cục tối giản, tập trung vào nội dung và khoảng trắng.", "~/Views/Templates/Minimal/Index.cshtml");
        await EnsureDerivedTemplateAsync("editorial", "Editorial", "Bố cục bất đối xứng, typography lớn và hình ảnh giàu tính biên tập.", "~/Views/Templates/Editorial/Index.cshtml");
        await EnsureDerivedTemplateAsync("full-width", "Full Width", "Bố cục tràn cạnh, ưu tiên hình ảnh lớn và chuyển tiếp mạnh giữa các section.", "~/Views/Templates/FullWidth/Index.cshtml");
        await EnsureDerivedTemplateAsync("conversion", "Conversion", "Bố cục cô đọng, ưu tiên bằng chứng, lời kêu gọi hành động và form liên hệ.", "~/Views/Templates/Conversion/Index.cshtml");

        await BackfillPageSectionsAsync(db);

        if (!await db.SiteTemplateSettings.AnyAsync())
        {
            db.SiteTemplateSettings.Add(new SiteTemplateSetting { ActiveTemplateId = template.Id });
            await db.SaveChangesAsync();
        }
        await SeedSettingsAsync(db);
        await SeedThemesAsync(db);
    }

    private static async Task BackfillPageSectionsAsync(ApplicationDbContext db)
    {
        var pageSections = await db.PageSections.ToDictionaryAsync(x => x.SectionKey);
        var slots = await db.TemplateSections.OrderBy(x => x.Id).ToListAsync();
        foreach (var slot in slots)
        {
            if (!pageSections.TryGetValue(slot.SectionKey, out var pageSection))
            {
                pageSection = new PageSection
                {
                    SectionKey = slot.SectionKey,
                    DisplayName = slot.DisplayName,
                    SectionDefinitionId = slot.SectionDefinitionId
                };
                db.PageSections.Add(pageSection);
                pageSections[slot.SectionKey] = pageSection;
            }
            slot.PageSection = pageSection;
        }

        var knownKeys = pageSections.Keys.ToArray();
        var orphanContents = await db.SectionContents
            .Where(x => !knownKeys.Contains(x.SectionKey)).ToListAsync();
        foreach (var content in orphanContents)
            db.PageSections.Add(new PageSection
            {
                SectionKey = content.SectionKey,
                DisplayName = content.SectionKey,
                SectionDefinitionId = content.SectionDefinitionId
            });

        await db.SaveChangesAsync();
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

        var templateKeys = new[] { "corporate", "minimal", "editorial", "full-width", "conversion" };
        var templates = await db.PageTemplates.Where(x => templateKeys.Contains(x.Key)).ToListAsync();
        var developerKeys = developerSettings.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        var links = await db.TemplateSettings.Where(x => templates.Select(t => t.Id).Contains(x.TemplateId)).ToListAsync();
        foreach (var template in templates)
            foreach (var definition in existing.Values.Where(x => developerKeys.Contains(x.Key)))
                if (!links.Any(x => x.TemplateId == template.Id && x.SettingDefinitionId == definition.Id))
                    db.TemplateSettings.Add(new TemplateSetting { TemplateId = template.Id, SettingDefinitionId = definition.Id, SortOrder = definition.SortOrder });
        await db.SaveChangesAsync();
    }

    private static async Task SeedThemesAsync(ApplicationDbContext db)
    {
        var developerThemes = new[]
        {
            new DeveloperTheme("ocean", "Ocean", "Xanh dương hiện đại, sáng và tin cậy.", 10, new()
            {
                ["brand"]="#2563eb", ["brandHover"]="#1d4ed8", ["brandContrast"]="#ffffff",
                ["pageBackground"]="#ffffff", ["surface"]="#ffffff", ["surfaceAlt"]="#f3f7ff",
                ["text"]="#101828", ["textMuted"]="#667085", ["border"]="#dbe4f0",
                ["headerBackground"]="rgba(255,255,255,.94)", ["headerText"]="#172033",
                ["heroBackground"]="#eef4ff", ["heroText"]="#101828", ["heroMuted"]="#526176",
                ["contrastBackground"]="#111827", ["contrastText"]="#ffffff",
                ["footerBackground"]="#0b1220", ["footerText"]="#ffffff", ["highlight"]="#dbeafe"
            }),
            new DeveloperTheme("emerald", "Emerald", "Xanh lá tự nhiên, cân bằng và bền vững.", 20, new()
            {
                ["brand"]="#07875b", ["brandHover"]="#056b49", ["brandContrast"]="#ffffff",
                ["pageBackground"]="#fbfdf9", ["surface"]="#ffffff", ["surfaceAlt"]="#eef7ef",
                ["text"]="#15231c", ["textMuted"]="#5c6d63", ["border"]="#d2e2d7",
                ["headerBackground"]="rgba(251,253,249,.94)", ["headerText"]="#15231c",
                ["heroBackground"]="#e4f4e8", ["heroText"]="#14251c", ["heroMuted"]="#53675a",
                ["contrastBackground"]="#123326", ["contrastText"]="#ffffff",
                ["footerBackground"]="#0c241a", ["footerText"]="#ffffff", ["highlight"]="#c8f1d8"
            }),
            new DeveloperTheme("sunset", "Sunset", "Cam đất ấm áp, thân thiện và giàu năng lượng.", 30, new()
            {
                ["brand"]="#e0522d", ["brandHover"]="#bd3e20", ["brandContrast"]="#ffffff",
                ["pageBackground"]="#fffaf5", ["surface"]="#ffffff", ["surfaceAlt"]="#fff0e5",
                ["text"]="#2b1c18", ["textMuted"]="#75615a", ["border"]="#ead7cd",
                ["headerBackground"]="rgba(255,250,245,.95)", ["headerText"]="#2b1c18",
                ["heroBackground"]="#fde5d4", ["heroText"]="#301b14", ["heroMuted"]="#76584d",
                ["contrastBackground"]="#3a211b", ["contrastText"]="#fffaf5",
                ["footerBackground"]="#291713", ["footerText"]="#fffaf5", ["highlight"]="#ffd3b8"
            }),
            new DeveloperTheme("monochrome", "Monochrome", "Đen trắng tối giản, sắc nét và tập trung vào nội dung.", 40, new()
            {
                ["brand"]="#171717", ["brandHover"]="#3f3f3f", ["brandContrast"]="#ffffff",
                ["pageBackground"]="#ffffff", ["surface"]="#ffffff", ["surfaceAlt"]="#f2f2f0",
                ["text"]="#111111", ["textMuted"]="#626262", ["border"]="#d4d4d1",
                ["headerBackground"]="rgba(255,255,255,.95)", ["headerText"]="#111111",
                ["heroBackground"]="#e9e9e5", ["heroText"]="#111111", ["heroMuted"]="#555555",
                ["contrastBackground"]="#111111", ["contrastText"]="#ffffff",
                ["footerBackground"]="#111111", ["footerText"]="#ffffff", ["highlight"]="#dddd3e"
            }),
            new DeveloperTheme("midnight", "Midnight", "Nền tối cao cấp với điểm nhấn tím hiện đại.", 50, new()
            {
                ["brand"]="#7c6cff", ["brandHover"]="#978cff", ["brandContrast"]="#ffffff",
                ["pageBackground"]="#0e1118", ["surface"]="#171c26", ["surfaceAlt"]="#121721",
                ["text"]="#f4f6fb", ["textMuted"]="#a7b0c0", ["border"]="#303847",
                ["headerBackground"]="rgba(14,17,24,.94)", ["headerText"]="#f4f6fb",
                ["heroBackground"]="#171c2d", ["heroText"]="#ffffff", ["heroMuted"]="#b9c1d1",
                ["contrastBackground"]="#7c6cff", ["contrastText"]="#ffffff",
                ["footerBackground"]="#080a10", ["footerText"]="#ffffff", ["highlight"]="#272e52"
            })
        };

        var existing = await db.ThemeDefinitions.ToDictionaryAsync(x => x.Key);
        foreach (var item in developerThemes)
        {
            if (!existing.TryGetValue(item.Key, out var theme))
            {
                theme = new ThemeDefinition { Key = item.Key };
                db.ThemeDefinitions.Add(theme);
                existing[item.Key] = theme;
            }
            theme.Name = item.Name;
            theme.Description = item.Description;
            theme.SortOrder = item.SortOrder;
            theme.TokensJson = JsonSerializer.Serialize(item.Tokens);
            theme.IsEnabled = true;
        }
        await db.SaveChangesAsync();

        if (!await db.SiteThemeSettings.AnyAsync())
        {
            db.SiteThemeSettings.Add(new SiteThemeSetting { ActiveThemeId = existing["ocean"].Id });
            await db.SaveChangesAsync();
        }
    }

    private const string TextContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"textarea"},"imageUrl":{"editor":"image"},"primaryButtonText":{"editor":"text"},"primaryButtonUrl":{"editor":"text"},"secondaryButtonText":{"editor":"text"},"secondaryButtonUrl":{"editor":"text"}},"navigation":{"allowed":false,"defaultVisible":false}}
        """;

    private const string HeroContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"textarea"},"imageUrl":{"editor":"image"},"primaryButtonText":{"editor":"text"},"primaryButtonUrl":{"editor":"text"},"secondaryButtonText":{"editor":"text"},"secondaryButtonUrl":{"editor":"text"}},"settings":{"layout":{"editor":"select","default":"default","options":[{"value":"default","label":"Mặc định"},{"value":"lead-form-right","label":"Form liên hệ bên phải"}]}},"navigation":{"allowed":false,"defaultVisible":false}}
        """;

    private const string GalleryContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"textarea"}},"settings":{"layout":{"editor":"select","default":"grid","options":[{"value":"grid","label":"Lưới hình ảnh"},{"value":"featured","label":"Một ảnh lớn, các ảnh nhỏ"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string FaqContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"}},"items":{"fields":{"question":{"label":"Câu hỏi","editor":"text","required":true},"answer":{"label":"Câu trả lời","editor":"html","htmlPolicy":"RichContent","required":true}}},"settings":{"layout":{"editor":"select","default":"single-column","options":[{"value":"single-column","label":"Một cột"},{"value":"two-columns","label":"Hai cột"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string TestimonialsContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"}},"items":{"fields":{"name":{"label":"Tên khách hàng","editor":"text","required":true},"position":{"label":"Chức danh / đơn vị","editor":"text"},"quote":{"label":"Ý kiến khách hàng","editor":"textarea","required":true},"image":{"label":"Ảnh đại diện","editor":"image"}}},"settings":{"layout":{"editor":"select","default":"grid","options":[{"value":"grid","label":"Dạng lưới"},{"value":"featured","label":"Một ý kiến nổi bật"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string ProcessContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"}},"items":{"fields":{"title":{"label":"Tên bước","editor":"text","required":true},"content":{"label":"Mô tả","editor":"textarea","required":true}}},"settings":{"layout":{"editor":"select","default":"horizontal","options":[{"value":"horizontal","label":"Theo chiều ngang"},{"value":"vertical","label":"Theo chiều dọc"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string MediaContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"}},"items":{"fields":{"title":{"label":"Tiêu đề video","editor":"text","required":true},"description":{"label":"Mô tả","editor":"textarea"},"mediaUrl":{"label":"URL YouTube hoặc Vimeo","editor":"media-url","required":true},"image":{"label":"Ảnh thumbnail","editor":"image"}}},"settings":{"layout":{"editor":"select","default":"grid","options":[{"value":"grid","label":"Dạng lưới"},{"value":"featured","label":"Video đầu tiên nổi bật"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string PricingContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"}},"items":{"fields":{"name":{"label":"Tên gói","editor":"text","required":true},"price":{"label":"Giá","editor":"text","required":true},"period":{"label":"Chu kỳ / ghi chú giá","editor":"text"},"description":{"label":"Mô tả ngắn","editor":"textarea"},"features":{"label":"Danh sách quyền lợi","editor":"html","htmlPolicy":"RichContent","required":true},"buttonText":{"label":"Nội dung nút","editor":"text"},"buttonUrl":{"label":"Liên kết nút","editor":"url"},"emphasis":{"label":"Mức độ nổi bật","editor":"select","options":[{"value":"normal","label":"Thông thường"},{"value":"featured","label":"Nổi bật"}]}}},"settings":{"layout":{"editor":"select","default":"cards","options":[{"value":"cards","label":"Dạng thẻ"},{"value":"compact","label":"Thu gọn"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string PartnersContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"}},"items":{"fields":{"name":{"label":"Tên đối tác / khách hàng","editor":"text","required":true},"url":{"label":"Liên kết website","editor":"url"},"image":{"label":"Logo","editor":"image","required":true}}},"settings":{"layout":{"editor":"select","default":"grid","options":[{"value":"grid","label":"Lưới logo"},{"value":"monochrome","label":"Logo đơn sắc"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string TeamContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"}},"items":{"fields":{"name":{"label":"Họ tên","editor":"text","required":true},"position":{"label":"Chức danh","editor":"text","required":true},"bio":{"label":"Giới thiệu ngắn","editor":"textarea"},"profileUrl":{"label":"Liên kết hồ sơ","editor":"url"},"image":{"label":"Ảnh thành viên","editor":"image","required":true}}},"settings":{"layout":{"editor":"select","default":"grid","options":[{"value":"grid","label":"Dạng lưới"},{"value":"compact","label":"Thu gọn"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string StructuredContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"structured-list"},"imageUrl":{"editor":"image"},"primaryButtonText":{"editor":"text"},"primaryButtonUrl":{"editor":"text"},"secondaryButtonText":{"editor":"text"},"secondaryButtonUrl":{"editor":"text"}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string NavigableTextContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"textarea"},"imageUrl":{"editor":"image"},"primaryButtonText":{"editor":"text"},"primaryButtonUrl":{"editor":"text"},"secondaryButtonText":{"editor":"text"},"secondaryButtonUrl":{"editor":"text"}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private const string RichContentSchema = """
        {"fields":{"eyebrow":{"editor":"text"},"title":{"editor":"text"},"subtitle":{"editor":"textarea"},"content":{"editor":"html","htmlPolicy":"RichContent"},"imageUrl":{"editor":"image"},"primaryButtonText":{"editor":"text"},"primaryButtonUrl":{"editor":"text"},"secondaryButtonText":{"editor":"text"},"secondaryButtonUrl":{"editor":"text"}},"settings":{"layout":{"editor":"select","default":"image-left","options":[{"value":"image-left","label":"Hình bên trái"},{"value":"image-right","label":"Hình bên phải"}]}},"navigation":{"allowed":true,"defaultVisible":false}}
        """;

    private static string GetDefaultDisplayName(string sectionKey, string fallback) => sectionKey switch
    {
        "hero" => "Hero", "services" => "Dịch vụ", "about" => "Giới thiệu",
        "stats" => "Số liệu", "contact" => "Liên hệ", _ => fallback
    };

    private static string? GetDefaultNavigationLabel(string sectionKey) => sectionKey switch
    {
        "services" => "Dịch vụ", "about" => "Về chúng tôi", "contact" => "Liên hệ", _ => null
    };

    private sealed record DeveloperSection(string Key, string Name, string SectionType, string SchemaJson);
    private sealed record DeveloperSetting(string Key, string Name, string Group, string ValueType, string Description, int SortOrder);
    private sealed record DeveloperTheme(string Key, string Name, string Description, int SortOrder, Dictionary<string, string> Tokens);
}
