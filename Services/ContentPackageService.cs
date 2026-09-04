using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using LandingCms.Data;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Services;

public interface IContentPackageService
{
    Task ExportAsync(Stream destination, CancellationToken cancellationToken = default);
    Task<ContentPackageInspection> InspectAsync(string packagePath, string token, CancellationToken cancellationToken = default);
    Task<string> ImportAsync(string packagePath, CancellationToken cancellationToken = default);
}

public sealed class ContentPackageService(
    ApplicationDbContext db, IWebHostEnvironment environment)
    : IContentPackageService
{
    public const long MaximumPackageBytes = 512L * 1024 * 1024;
    private const long MaximumExpandedBytes = 1024L * 1024 * 1024;
    private const int MaximumEntries = 5000;
    private const string ManifestEntryName = "manifest.json";
    private const string ContentEntryName = "content.json";
    private const string ChecksumsEntryName = "checksums.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public async Task ExportAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        var data = await LoadDataAsync(cancellationToken);
        SanitizeAuditFields(data);
        var site = data.SiteSettings.FirstOrDefault();
        var templateSetting = data.SiteTemplateSettings.FirstOrDefault();
        var themeSetting = data.SiteThemeSettings.FirstOrDefault();
        var manifest = new ContentPackageManifest
        {
            SiteName = site?.SiteName,
            ActiveTemplateKey = data.PageTemplates.FirstOrDefault(x => x.Id == templateSetting?.ActiveTemplateId)?.Key,
            ActiveThemeKey = data.ThemeDefinitions.FirstOrDefault(x => x.Id == themeSetting?.ActiveThemeId)?.Key,
            SectionCount = data.PageSections.Count,
            MediaCount = data.MediaAssets.Count(x => !x.IsDeleted),
            MediaBytes = data.MediaAssets.Where(x => !x.IsDeleted).Sum(x => x.FileSize)
        };

        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, true);
        var checksums = new Dictionary<string, string>(StringComparer.Ordinal);
        await WriteJsonEntryAsync(archive, ManifestEntryName, manifest, cancellationToken);
        await WriteJsonEntryAsync(archive, ContentEntryName, data, cancellationToken);

        foreach (var asset in data.MediaAssets.Where(x => !x.IsDeleted))
        {
            var sourcePath = ResolveMediaPath(asset.RelativeUrl);
            if (sourcePath is null || !File.Exists(sourcePath)) continue;
            var entryName = GetMediaEntryName(asset.Id, asset.StoredFileName);
            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
            await using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            await using var output = entry.Open();
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                hash.AppendData(buffer, 0, read);
            }
            checksums[entryName] = Convert.ToHexString(hash.GetHashAndReset());
        }
        await WriteJsonEntryAsync(archive, ChecksumsEntryName, checksums, cancellationToken);
    }

    public async Task<ContentPackageInspection> InspectAsync(
        string packagePath, string token, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(packagePath);
        if (!fileInfo.Exists || fileInfo.Length == 0 || fileInfo.Length > MaximumPackageBytes)
            throw new InvalidOperationException("Package phải có dung lượng từ 1 byte đến 512 MB.");

        using var archive = ZipFile.OpenRead(packagePath);
        ValidateArchiveShape(archive);
        var manifest = await ReadJsonEntryAsync<ContentPackageManifest>(archive, ManifestEntryName, cancellationToken);
        ValidateManifest(manifest);
        var data = await ReadJsonEntryAsync<ContentPackageData>(archive, ContentEntryName, cancellationToken);
        await ValidateMediaAsync(archive, data, cancellationToken);
        var warnings = new List<string>();
        var missingFiles = data.MediaAssets.Count(x => !x.IsDeleted && archive.GetEntry(GetMediaEntryName(x.Id, x.StoredFileName)) is null);
        if (missingFiles > 0) warnings.Add($"Có {missingFiles} media không còn file vật lý trong package.");
        if (data.ContentLanguages.All(x => !x.IsDefault)) warnings.Add("Package không xác định ngôn ngữ mặc định.");
        return new ContentPackageInspection(token, manifest, data.ContentLanguages.Count, data.PageTemplates.Count,
            data.ThemeDefinitions.Count, data.SectionItems.Count, fileInfo.Length, warnings);
    }

    public async Task<string> ImportAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var token = Path.GetFileNameWithoutExtension(packagePath);
        _ = await InspectAsync(packagePath, token, cancellationToken);
        using var archive = ZipFile.OpenRead(packagePath);
        var data = await ReadJsonEntryAsync<ContentPackageData>(archive, ContentEntryName, cancellationToken);

        var backupDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "content-backups");
        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(backupDirectory, $"before-import-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.vcms.zip");
        await using (var backup = new FileStream(backupPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            await ExportAsync(backup, cancellationToken);

        var stagedDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "content-imports", $"stage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagedDirectory);
        var installedFiles = new List<(string Target, string? OriginalBackup)>();
        try
        {
            await StageAndInstallMediaAsync(archive, data, stagedDirectory, installedFiles, cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await ClearContentAsync(cancellationToken);
            AddPackageData(data);
            db.ContentRevisions.Add(new Models.ContentRevision
            {
                EntityType = "ContentPackage", EntityKey = Path.GetFileName(packagePath), Action = "Imported",
                DisplayName = "Khôi phục VCMS Content Package", CreatedByName = "SuperAdministrator",
                SnapshotJson = JsonSerializer.Serialize(new { BackupFile = Path.GetFileName(backupPath) })
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return backupPath;
        }
        catch
        {
            RestoreInstalledFiles(installedFiles);
            throw;
        }
        finally
        {
            try
            {
                if (Directory.Exists(stagedDirectory)) Directory.Delete(stagedDirectory, true);
            }
            catch { /* Cleanup must not turn a committed import into a reported failure. */ }
        }
    }

    private async Task<ContentPackageData> LoadDataAsync(CancellationToken ct) => new()
    {
        LandingSections = await db.LandingSections.AsNoTracking().ToListAsync(ct),
        SiteSettings = await db.SiteSettings.AsNoTracking().ToListAsync(ct),
        SiteSettingTranslations = await db.SiteSettingTranslations.AsNoTracking().ToListAsync(ct),
        PageTemplates = await db.PageTemplates.AsNoTracking().ToListAsync(ct),
        SectionDefinitions = await db.SectionDefinitions.AsNoTracking().ToListAsync(ct),
        PageSections = await db.PageSections.AsNoTracking().ToListAsync(ct),
        TemplateSections = await db.TemplateSections.AsNoTracking().ToListAsync(ct),
        TemplateSectionTranslations = await db.TemplateSectionTranslations.AsNoTracking().ToListAsync(ct),
        SectionContents = await db.SectionContents.AsNoTracking().ToListAsync(ct),
        SectionContentTranslations = await db.SectionContentTranslations.AsNoTracking().ToListAsync(ct),
        SectionItems = await db.SectionItems.AsNoTracking().ToListAsync(ct),
        SectionItemTranslations = await db.SectionItemTranslations.AsNoTracking().ToListAsync(ct),
        SiteTemplateSettings = await db.SiteTemplateSettings.AsNoTracking().ToListAsync(ct),
        ThemeDefinitions = await db.ThemeDefinitions.AsNoTracking().ToListAsync(ct),
        SiteThemeSettings = await db.SiteThemeSettings.AsNoTracking().ToListAsync(ct),
        SettingDefinitions = await db.SettingDefinitions.AsNoTracking().ToListAsync(ct),
        SettingValues = await db.SettingValues.AsNoTracking().ToListAsync(ct),
        TemplateSettings = await db.TemplateSettings.AsNoTracking().ToListAsync(ct),
        MediaAssets = await db.MediaAssets.AsNoTracking().ToListAsync(ct),
        SectionMedia = await db.SectionMedia.AsNoTracking().ToListAsync(ct),
        ContentLanguages = await db.ContentLanguages.AsNoTracking().ToListAsync(ct)
    };

    private async Task ClearContentAsync(CancellationToken ct)
    {
        await db.TemplateSectionTranslations.ExecuteDeleteAsync(ct);
        await db.SectionItemTranslations.ExecuteDeleteAsync(ct);
        await db.SectionContentTranslations.ExecuteDeleteAsync(ct);
        await db.SiteSettingTranslations.ExecuteDeleteAsync(ct);
        await db.TemplateSettings.ExecuteDeleteAsync(ct);
        await db.SectionMedia.ExecuteDeleteAsync(ct);
        await db.SectionItems.ExecuteDeleteAsync(ct);
        await db.TemplateSections.ExecuteDeleteAsync(ct);
        await db.SectionContents.ExecuteDeleteAsync(ct);
        await db.PageSections.ExecuteDeleteAsync(ct);
        await db.SiteTemplateSettings.ExecuteDeleteAsync(ct);
        await db.SiteThemeSettings.ExecuteDeleteAsync(ct);
        await db.SettingValues.ExecuteDeleteAsync(ct);
        await db.ThemeDefinitions.ExecuteUpdateAsync(x => x.SetProperty(t => t.BaseThemeId, (int?)null), ct);
        await db.ThemeDefinitions.ExecuteDeleteAsync(ct);
        await db.MediaAssets.ExecuteDeleteAsync(ct);
        await db.SettingDefinitions.ExecuteDeleteAsync(ct);
        await db.SectionDefinitions.ExecuteDeleteAsync(ct);
        await db.PageTemplates.ExecuteDeleteAsync(ct);
        await db.SiteSettings.ExecuteDeleteAsync(ct);
        await db.ContentLanguages.ExecuteDeleteAsync(ct);
        await db.LandingSections.ExecuteDeleteAsync(ct);
    }

    private void AddPackageData(ContentPackageData data)
    {
        db.ContentLanguages.AddRange(data.ContentLanguages);
        db.SiteSettings.AddRange(data.SiteSettings);
        db.PageTemplates.AddRange(data.PageTemplates);
        db.SectionDefinitions.AddRange(data.SectionDefinitions);
        db.SettingDefinitions.AddRange(data.SettingDefinitions);
        db.MediaAssets.AddRange(data.MediaAssets);
        db.ThemeDefinitions.AddRange(data.ThemeDefinitions);
        db.PageSections.AddRange(data.PageSections);
        db.SectionContents.AddRange(data.SectionContents);
        db.SectionItems.AddRange(data.SectionItems);
        db.TemplateSections.AddRange(data.TemplateSections);
        db.SettingValues.AddRange(data.SettingValues);
        db.TemplateSettings.AddRange(data.TemplateSettings);
        db.SectionMedia.AddRange(data.SectionMedia);
        db.SiteTemplateSettings.AddRange(data.SiteTemplateSettings);
        db.SiteThemeSettings.AddRange(data.SiteThemeSettings);
        db.SiteSettingTranslations.AddRange(data.SiteSettingTranslations);
        db.SectionContentTranslations.AddRange(data.SectionContentTranslations);
        db.SectionItemTranslations.AddRange(data.SectionItemTranslations);
        db.TemplateSectionTranslations.AddRange(data.TemplateSectionTranslations);
        db.LandingSections.AddRange(data.LandingSections);
    }

    private async Task StageAndInstallMediaAsync(ZipArchive archive, ContentPackageData data, string stage,
        List<(string Target, string? OriginalBackup)> installed, CancellationToken ct)
    {
        foreach (var asset in data.MediaAssets.Where(x => !x.IsDeleted))
        {
            var entry = archive.GetEntry(GetMediaEntryName(asset.Id, asset.StoredFileName));
            if (entry is null) continue;
            var target = ResolveMediaPath(asset.RelativeUrl)
                ?? throw new InvalidOperationException($"Đường dẫn media không hợp lệ: {asset.RelativeUrl}");
            var staged = Path.Combine(stage, $"{asset.Id}-{Path.GetFileName(asset.StoredFileName)}");
            await using (var input = entry.Open())
            await using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                await input.CopyToAsync(output, ct);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            string? oldBackup = null;
            if (File.Exists(target))
            {
                oldBackup = staged + ".old";
                File.Move(target, oldBackup);
            }
            try
            {
                File.Move(staged, target);
                installed.Add((target, oldBackup));
            }
            catch
            {
                if (oldBackup is not null && File.Exists(oldBackup)) File.Move(oldBackup, target);
                throw;
            }
        }
    }

    private static void RestoreInstalledFiles(IEnumerable<(string Target, string? OriginalBackup)> installed)
    {
        foreach (var item in installed.Reverse())
        {
            if (File.Exists(item.Target)) File.Delete(item.Target);
            if (item.OriginalBackup is not null && File.Exists(item.OriginalBackup)) File.Move(item.OriginalBackup, item.Target);
        }
    }

    private async Task ValidateMediaAsync(ZipArchive archive, ContentPackageData data, CancellationToken ct)
    {
        var checksums = await ReadJsonEntryAsync<Dictionary<string, string>>(archive, ChecksumsEntryName, ct);
        foreach (var asset in data.MediaAssets.Where(x => !x.IsDeleted))
        {
            if (ResolveMediaPath(asset.RelativeUrl) is null)
                throw new InvalidOperationException($"Package chứa đường dẫn media không hợp lệ: {asset.RelativeUrl}");
            var name = GetMediaEntryName(asset.Id, asset.StoredFileName);
            var entry = archive.GetEntry(name);
            if (entry is null) continue;
            if (!checksums.TryGetValue(name, out var expected))
                throw new InvalidOperationException($"Media {asset.OriginalFileName} không có checksum.");
            await using var stream = entry.Open();
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Media {asset.OriginalFileName} không vượt qua kiểm tra toàn vẹn.");
        }
    }

    private static void ValidateArchiveShape(ZipArchive archive)
    {
        if (archive.Entries.Count > MaximumEntries) throw new InvalidOperationException("Package chứa quá nhiều file.");
        var expanded = archive.Entries.Sum(x => x.Length);
        if (expanded > MaximumExpandedBytes) throw new InvalidOperationException("Dung lượng giải nén của package vượt quá 1 GB.");
        foreach (var entry in archive.Entries)
        {
            var normalized = entry.FullName.Replace('\\', '/');
            if (normalized.StartsWith('/') || normalized.Contains("../", StringComparison.Ordinal) || Path.IsPathRooted(normalized))
                throw new InvalidOperationException("Package chứa đường dẫn không an toàn.");
        }
    }

    private static void ValidateManifest(ContentPackageManifest manifest)
    {
        if (manifest.Format != "VCMS.ContentPackage") throw new InvalidOperationException("Đây không phải VCMS Content Package.");
        if (manifest.SchemaVersion != ContentPackageManifest.CurrentSchemaVersion)
            throw new InvalidOperationException($"Schema package {manifest.SchemaVersion} chưa được hỗ trợ.");
    }

    private string? ResolveMediaPath(string relativeUrl)
    {
        var normalized = relativeUrl.Replace('\\', '/');
        if (!normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || normalized.Contains("../", StringComparison.Ordinal)) return null;
        var root = Path.GetFullPath(environment.WebRootPath);
        var path = Path.GetFullPath(Path.Combine(root, normalized.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        return path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? path : null;
    }

    private static string GetMediaEntryName(long id, string storedName) => $"media/{id}/{Path.GetFileName(storedName)}";

    private static void SanitizeAuditFields(ContentPackageData data)
    {
        foreach (var item in data.SectionContents) item.UpdatedById = null;
        foreach (var item in data.SectionContentTranslations) item.UpdatedById = null;
        foreach (var item in data.SectionItems) item.UpdatedById = null;
        foreach (var item in data.SectionItemTranslations) item.UpdatedById = null;
        foreach (var item in data.SiteSettingTranslations) item.UpdatedById = null;
        foreach (var item in data.SettingValues) item.UpdatedById = null;
        foreach (var item in data.MediaAssets) item.UploadedById = null;
        foreach (var item in data.ThemeDefinitions) { item.CreatedBy = null; item.UpdatedBy = null; }
    }

    private static async Task WriteJsonEntryAsync<T>(ZipArchive archive, string name, T value, CancellationToken ct)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        await using var stream = entry.Open();
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, ct);
    }

    private static async Task<T> ReadJsonEntryAsync<T>(ZipArchive archive, string name, CancellationToken ct)
    {
        var entry = archive.GetEntry(name) ?? throw new InvalidOperationException($"Package thiếu {name}.");
        await using var stream = entry.Open();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct)
            ?? throw new InvalidOperationException($"Không thể đọc {name}.");
    }
}
