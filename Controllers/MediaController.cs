using LandingCms.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System.Collections.Concurrent;

namespace LandingCms.Controllers;

[Route("media")]
public sealed class MediaController(ApplicationDbContext db, IWebHostEnvironment environment) : Controller
{
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> ThumbnailLocks = new();
    private const int ThumbnailMaxWidth = 800;
    private const int ThumbnailMaxHeight = 600;

    [HttpGet("{id:long}/thumbnail")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Thumbnail(long id, CancellationToken cancellationToken)
    {
        var asset = await db.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (asset is null || !asset.RelativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var uploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));
        var sourcePath = Path.GetFullPath(Path.Combine(environment.WebRootPath, asset.RelativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (!sourcePath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(sourcePath))
            return NotFound();

        var thumbnailPath = Path.Combine(
            Path.GetDirectoryName(sourcePath)!,
            Path.GetFileNameWithoutExtension(sourcePath) + ".thumb.webp");

        if (!System.IO.File.Exists(thumbnailPath))
        {
            var thumbnailLock = ThumbnailLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
            await thumbnailLock.WaitAsync(cancellationToken);
            try
            {
                if (!System.IO.File.Exists(thumbnailPath))
                    await CreateThumbnailAsync(sourcePath, thumbnailPath, cancellationToken);
            }
            finally
            {
                thumbnailLock.Release();
            }
        }

        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        return PhysicalFile(thumbnailPath, "image/webp");
    }

    private static async Task CreateThumbnailAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        using var source = SKBitmap.Decode(sourcePath) ?? throw new InvalidOperationException("Không thể tạo thumbnail cho hình ảnh.");
        var scale = Math.Min(1d, Math.Min((double)ThumbnailMaxWidth / source.Width, (double)ThumbnailMaxHeight / source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var resized = scale < 1 ? source.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium) : source.Copy();
        if (resized is null) throw new InvalidOperationException("Không thể thay đổi kích thước thumbnail.");
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 72) ?? throw new InvalidOperationException("Không thể mã hóa thumbnail.");
        await using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true);
        encoded.SaveTo(output);
        await output.FlushAsync(cancellationToken);
    }
}
