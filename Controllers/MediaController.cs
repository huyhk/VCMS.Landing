using LandingCms.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LandingCms.Services;
using System.Collections.Concurrent;

namespace LandingCms.Controllers;

[Route("media")]
public sealed class MediaController(ApplicationDbContext db, IWebHostEnvironment environment) : Controller
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> VariantLocks = new();
    [HttpGet("{id:long}/thumbnail")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> Thumbnail(long id, CancellationToken cancellationToken) =>
        ServeVariantAsync(id, "thumbnail", 800, cancellationToken);

    [HttpGet("{id:long}/thumbnail/{width:int}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> ThumbnailVariant(long id, int width, CancellationToken cancellationToken) =>
        ServeVariantAsync(id, "thumbnail", width, cancellationToken);

    [HttpGet("{id:long}/hero")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> Hero(long id, CancellationToken cancellationToken) =>
        ServeVariantAsync(id, "hero", 1600, cancellationToken);

    [HttpGet("{id:long}/hero/{width:int}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> HeroVariant(long id, int width, CancellationToken cancellationToken) =>
        ServeVariantAsync(id, "hero", width, cancellationToken);

    private async Task<IActionResult> ServeVariantAsync(long id, string profile, int width, CancellationToken cancellationToken)
    {
        if (!ImageVariants.Supports(profile, width)) return NotFound();
        var sourcePath = await ResolveSourcePathAsync(id, cancellationToken);
        if (sourcePath is null) return NotFound();
        var variantPath = ImageVariants.GetPath(sourcePath, profile, width);
        if (!System.IO.File.Exists(variantPath))
        {
            var variantLock = VariantLocks.GetOrAdd($"{profile}:{id}:{width}", _ => new SemaphoreSlim(1, 1));
            await variantLock.WaitAsync(cancellationToken);
            try
            {
                if (!System.IO.File.Exists(variantPath))
                    await ImageVariants.CreateAsync(sourcePath, variantPath, width, ImageVariants.Quality(profile), cancellationToken);
            }
            finally
            {
                variantLock.Release();
            }
        }
        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        return PhysicalFile(variantPath, "image/webp");
    }

    private async Task<string?> ResolveSourcePathAsync(long id, CancellationToken cancellationToken)
    {
        var asset = await db.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (asset is null || !asset.RelativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return null;
        var uploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));
        var sourcePath = Path.GetFullPath(Path.Combine(environment.WebRootPath, asset.RelativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        return sourcePath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(sourcePath)
            ? sourcePath
            : null;
    }

}
