using LandingCms.Data;
using LandingCms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using LandingCms.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using System.Globalization;
using System.Threading.RateLimiting;
using VNS.Licensing.Client.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ContentPackageService.MaximumPackageBytes);
builder.Services.Configure<Microsoft.AspNetCore.Builder.IISServerOptions>(options =>
    options.MaxRequestBodySize = ContentPackageService.MaximumPackageBytes);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    options.MultipartBodyLengthLimit = ContentPackageService.MaximumPackageBytes);

var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(configuredConnectionString))
    configuredConnectionString = "Data Source=App_Data/landing.db";
var sqliteConnection = new SqliteConnectionStringBuilder(configuredConnectionString);
if (!Path.IsPathRooted(sqliteConnection.DataSource))
    sqliteConnection.DataSource = Path.GetFullPath(sqliteConnection.DataSource, builder.Environment.ContentRootPath);
var databaseDirectory = Path.GetDirectoryName(sqliteConnection.DataSource);
if (!string.IsNullOrWhiteSpace(databaseDirectory))
    Directory.CreateDirectory(databaseDirectory);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteConnection.ConnectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/account/login";
    options.AccessDeniedPath = "/admin/account/access-denied";
    options.Cookie.Name = "LandingCms.Auth";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        CultureInfo.GetCultureInfo("vi"),
        CultureInfo.GetCultureInfo("en"),
        CultureInfo.GetCultureInfo("zh")
    };
    options.DefaultRequestCulture = new RequestCulture("vi");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders =
    [
        new RouteDataRequestCultureProvider
        {
            RouteDataStringKey = "culture",
            UIRouteDataStringKey = "culture"
        }
    ];
});
builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddMemoryCache();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var value in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        if (IPAddress.TryParse(value, out var address)) options.KnownProxies.Add(address);
});
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<AiContentOptions>(builder.Configuration.GetSection("AI"));
builder.Services.AddOptions<CloudflareTurnstileOptions>()
    .Bind(builder.Configuration.GetSection("CloudflareTurnstile"))
    .Validate(options => options.HasSiteKey == options.HasSecretKey,
        "Cloudflare Turnstile requires both SiteKey and SecretKey.")
    .ValidateOnStart();
builder.Services.AddVnsLicensing(builder.Configuration);
builder.Services.AddScoped<IContactEmailSender, ContactEmailSender>();
builder.Services.AddHttpClient<ICloudflareTurnstileValidator, CloudflareTurnstileValidator>(client =>
    client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddScoped<IMediaStorageService, MediaStorageService>();
builder.Services.AddScoped<IContentPackageService, ContentPackageService>();
builder.Services.AddSingleton<IAiDraftStore, AiDraftStore>();
builder.Services.AddHttpClient<IAiContentService, OpenAiContentService>((services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiContentOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 30, 300));
});
builder.Services.AddSingleton<IContentHtmlSanitizer, ContentHtmlSanitizer>();
builder.Services.AddSingleton<ISectionSchemaService, SectionSchemaService>();
builder.Services.AddSingleton<IChromeLayoutService, ChromeLayoutService>();
builder.Services.AddSingleton<ITemplateStyleProvider, TemplateStyleProvider>();
builder.Services.AddSingleton<IThemeCssService, ThemeCssService>();
builder.Services.AddSingleton<IDeploymentVersionProvider, DeploymentVersionProvider>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("contact", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("ai-content", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromHours(1), QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();
app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        context.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
        return Task.CompletedTask;
    });
    await next();
});
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path;
        if (path.StartsWithSegments("/uploads") || context.Context.Request.Query.ContainsKey("v"))
        {
            context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        }
        else if (path.StartsWithSegments("/css") || path.StartsWithSegments("/js"))
        {
            context.Context.Response.Headers.CacheControl = "public,max-age=86400";
        }
    }
});
app.UseRouting();
app.Map("/health", healthApp => healthApp.Run(async context =>
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync("""{"status":"Healthy"}""");
}));
app.UseRequestLocalization();
app.UseVnsLicensing();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(name: "localized-home", pattern: "{culture}",
    defaults: new { controller = "Home", action = "Index" },
    constraints: new { culture = "^[a-z]{2}(-[A-Z]{2})?$" });
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Logger.LogInformation("Using SQLite database at {DatabasePath}.", sqliteConnection.DataSource);
await DbInitializer.InitializeAsync(app.Services, app.Configuration);
app.Run();
