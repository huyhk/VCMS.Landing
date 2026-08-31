using LandingCms.Data;
using LandingCms.Models;
using SkiaSharp;

namespace LandingCms.Services;

public enum ImageUploadProfile { General, HeroBackground, SectionImage, Logo, Favicon }

public interface IMediaStorageService
{
    Task<MediaAsset> SaveImageAsync(IFormFile file, string? userId, ImageUploadProfile profile, CancellationToken cancellationToken = default);
}

public class MediaStorageService(IWebHostEnvironment environment, ApplicationDbContext db) : IMediaStorageService
{
    public const long MaximumFileSize = 5 * 1024 * 1024;
    public const long MaximumPixelCount = 30_000_000;

    public async Task<MediaAsset> SaveImageAsync(IFormFile file, string? userId, ImageUploadProfile profile, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > MaximumFileSize)
            throw new InvalidOperationException("Ảnh phải có dung lượng từ 1 byte đến 5 MB.");

        await using var input = file.OpenReadStream();
        using var codec = SKCodec.Create(input, out var codecResult);
        if (codec is null || codecResult != SKCodecResult.Success || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new InvalidOperationException("Chỉ hỗ trợ ảnh PNG, JPEG hoặc WebP hợp lệ.");
        var sourceInfo = codec.Info;
        if (sourceInfo.Width <= 0 || sourceInfo.Height <= 0 || (long)sourceInfo.Width * sourceInfo.Height > MaximumPixelCount)
            throw new InvalidOperationException("Ảnh có độ phân giải quá lớn. Tối đa 30 triệu pixel.");

        using var decoded = SKBitmap.Decode(codec) ?? throw new InvalidOperationException("Không thể giải mã file ảnh.");
        using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);
        var limits = GetLimits(profile);
        var scale = Math.Min(1d, Math.Min((double)limits.Width / oriented.Width, (double)limits.Height / oriented.Height));
        var targetWidth = Math.Max(1, (int)Math.Round(oriented.Width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(oriented.Height * scale));
        using var resized = scale < 1
            ? oriented.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.High)
            : oriented.Copy();
        if (resized is null) throw new InvalidOperationException("Không thể thay đổi kích thước ảnh.");

        var keepPng = (profile is ImageUploadProfile.Logo or ImageUploadProfile.Favicon) && codec.EncodedFormat == SKEncodedImageFormat.Png;
        var outputFormat = keepPng ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Webp;
        var extension = keepPng ? ".png" : ".webp";
        var contentType = keepPng ? "image/png" : "image/webp";
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(outputFormat, keepPng ? 100 : GetQuality(profile))
            ?? throw new InvalidOperationException("Không thể tối ưu file ảnh.");

        var now = DateTime.UtcNow;
        var relativeDirectory = $"uploads/{now:yyyy}/{now:MM}";
        var physicalDirectory = Path.Combine(environment.WebRootPath, "uploads", now.ToString("yyyy"), now.ToString("MM"));
        Directory.CreateDirectory(physicalDirectory);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(physicalDirectory, storedName);
        var createdFiles = new List<string>();
        try
        {
            await using (var destination = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                encoded.SaveTo(destination);
                await destination.FlushAsync(cancellationToken);
            }
            createdFiles.Add(physicalPath);
            if (profile == ImageUploadProfile.HeroBackground)
            {
                foreach (var width in ImageVariants.HeroWidths)
                {
                    var variantPath = ImageVariants.GetPath(physicalPath, "hero", width);
                    createdFiles.Add(variantPath);
                    await ImageVariants.CreateAsync(resized, variantPath, width, ImageVariants.Quality("hero"), cancellationToken);
                }
            }
            else if (profile == ImageUploadProfile.Logo)
            {
                foreach (var width in ImageVariants.ThumbnailWidths)
                {
                    var variantPath = ImageVariants.GetPath(physicalPath, "thumbnail", width);
                    createdFiles.Add(variantPath);
                    await ImageVariants.CreateAsync(resized, variantPath, width, ImageVariants.Quality("thumbnail"), cancellationToken);
                }
            }
            var asset = new MediaAsset
            {
                OriginalFileName = Path.GetFileName(file.FileName), StoredFileName = storedName,
                RelativeUrl = $"/{relativeDirectory}/{storedName}", ContentType = contentType,
                FileSize = new FileInfo(physicalPath).Length, Width = targetWidth, Height = targetHeight,
                UploadedById = userId
            };
            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(cancellationToken);
            return asset;
        }
        catch
        {
            foreach (var path in createdFiles)
                if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    private static (int Width, int Height) GetLimits(ImageUploadProfile profile) => profile switch
    {
        ImageUploadProfile.HeroBackground => (1920, 1200),
        ImageUploadProfile.SectionImage => (1400, 1400),
        ImageUploadProfile.Logo => (1000, 500),
        ImageUploadProfile.Favicon => (256, 256),
        _ => (1920, 1920)
    };

    private static int GetQuality(ImageUploadProfile profile) => profile switch
    {
        ImageUploadProfile.HeroBackground => 74,
        ImageUploadProfile.SectionImage => 76,
        _ => 80
    };

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapDimensions = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var result = new SKBitmap(swapDimensions ? source.Height : source.Width, swapDimensions ? source.Width : source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        var width = source.Width;
        var height = source.Height;
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
            _ => SKMatrix.Identity
        };
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(source, 0, 0);
        canvas.Flush();
        return result;
    }
}
