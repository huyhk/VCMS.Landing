using System.Buffers.Binary;
using LandingCms.Data;
using LandingCms.Models;

namespace LandingCms.Services;

public interface IMediaStorageService
{
    Task<MediaAsset> SaveImageAsync(IFormFile file, string? userId, CancellationToken cancellationToken = default);
}

public class MediaStorageService(IWebHostEnvironment environment, ApplicationDbContext db) : IMediaStorageService
{
    public const long MaximumFileSize = 5 * 1024 * 1024;

    public async Task<MediaAsset> SaveImageAsync(IFormFile file, string? userId, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > MaximumFileSize)
            throw new InvalidOperationException("Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
        byte[] header = new byte[32];
        await using (var input = file.OpenReadStream())
        {
            var read = await input.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < 12) throw new InvalidOperationException("File ảnh không hợp lệ.");
        }
        var format = DetectFormat(header) ?? throw new InvalidOperationException("Chỉ hỗ trợ ảnh PNG, JPEG hoặc WebP.");
        var now = DateTime.UtcNow;
        var relativeDirectory = $"uploads/{now:yyyy}/{now:MM}";
        var physicalDirectory = Path.Combine(environment.WebRootPath, "uploads", now.ToString("yyyy"), now.ToString("MM"));
        Directory.CreateDirectory(physicalDirectory);
        var storedName = $"{Guid.NewGuid():N}{format.Extension}";
        var physicalPath = Path.Combine(physicalDirectory, storedName);
        try
        {
            await using (var source = file.OpenReadStream())
            await using (var destination = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                await source.CopyToAsync(destination, cancellationToken);
            var asset = new MediaAsset
            {
                OriginalFileName = Path.GetFileName(file.FileName), StoredFileName = storedName,
                RelativeUrl = $"/{relativeDirectory}/{storedName}", ContentType = format.ContentType,
                FileSize = file.Length, Width = format.Width, Height = format.Height,
                UploadedById = userId
            };
            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(cancellationToken);
            return asset;
        }
        catch
        {
            if (File.Exists(physicalPath)) File.Delete(physicalPath);
            throw;
        }
    }

    private static ImageFormat? DetectFormat(byte[] header)
    {
        if (header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return new ImageFormat(".png", "image/png", BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4)));
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return new ImageFormat(".jpg", "image/jpeg", null, null);
        if (header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return new ImageFormat(".webp", "image/webp", null, null);
        return null;
    }

    private sealed record ImageFormat(string Extension, string ContentType, int? Width, int? Height);
}
