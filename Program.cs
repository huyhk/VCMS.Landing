using LandingCms.Data;
using LandingCms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LandingCms.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
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
builder.Services.AddControllersWithViews();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddOptions<CloudflareTurnstileOptions>()
    .Bind(builder.Configuration.GetSection("CloudflareTurnstile"))
    .Validate(options => options.HasSiteKey == options.HasSecretKey,
        "Cloudflare Turnstile requires both SiteKey and SecretKey.")
    .ValidateOnStart();
builder.Services.AddScoped<IContactEmailSender, ContactEmailSender>();
builder.Services.AddHttpClient<ICloudflareTurnstileValidator, CloudflareTurnstileValidator>(client =>
    client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddScoped<IMediaStorageService, MediaStorageService>();
builder.Services.AddSingleton<IContentHtmlSanitizer, ContentHtmlSanitizer>();
builder.Services.AddSingleton<ISectionSchemaService, SectionSchemaService>();
builder.Services.AddSingleton<ITemplateStyleProvider, TemplateStyleProvider>();
builder.Services.AddRateLimiter(options => options.AddPolicy("contact", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 })));

var app = builder.Build();
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await DbInitializer.InitializeAsync(app.Services, app.Configuration);
app.Run();
