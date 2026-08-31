using SkiaSharp;

namespace LandingCms.Services;

public static class ImageVariants
{
    public static readonly int[] HeroWidths = [640, 960, 1280, 1600];
    public static readonly int[] ThumbnailWidths = [320, 480, 800];

    public static bool Supports(string profile, int width) => profile switch
    {
        "hero" => HeroWidths.Contains(width),
        "thumbnail" => ThumbnailWidths.Contains(width),
        _ => false
    };

    public static int Quality(string profile) => profile == "hero" ? 62 : 72;

    public static string GetPath(string sourcePath, string profile, int width) => Path.Combine(
        Path.GetDirectoryName(sourcePath)!,
        $"{Path.GetFileNameWithoutExtension(sourcePath)}.{profile}-{width}.webp");

    public static async Task CreateAsync(string sourcePath, string destinationPath, int width, int quality,
        CancellationToken cancellationToken = default)
    {
        using var source = SKBitmap.Decode(sourcePath)
            ?? throw new InvalidOperationException("Không thể tạo biến thể hình ảnh.");
        await CreateAsync(source, destinationPath, width, quality, cancellationToken);
    }

    public static async Task CreateAsync(SKBitmap source, string destinationPath, int requestedWidth, int quality,
        CancellationToken cancellationToken = default)
    {
        var scale = Math.Min(1d, (double)requestedWidth / source.Width);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var resized = scale < 1
            ? source.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium)
            : source.Copy();
        if (resized is null) throw new InvalidOperationException("Không thể thay đổi kích thước hình ảnh.");
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, quality)
            ?? throw new InvalidOperationException("Không thể mã hóa biến thể hình ảnh.");
        await using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true);
        encoded.SaveTo(output);
        await output.FlushAsync(cancellationToken);
    }
}
