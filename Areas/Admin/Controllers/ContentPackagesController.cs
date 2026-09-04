using LandingCms.Data;
using LandingCms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = DbInitializer.SuperAdministrator)]
public sealed class ContentPackagesController(
    IContentPackageService packages, IWebHostEnvironment environment, ILogger<ContentPackagesController> logger) : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data", "content-exports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"vcms-content-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.vcms.zip");
        await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            await packages.ExportAsync(stream, cancellationToken);
        Response.OnCompleted(() =>
        {
            try { System.IO.File.Delete(path); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not delete temporary content export {Path}", path); }
            return Task.CompletedTask;
        });
        return PhysicalFile(path, "application/zip", $"vcms-content-{DateTime.Now:yyyyMMdd-HHmmss}.vcms.zip", true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(ContentPackageService.MaximumPackageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ContentPackageService.MaximumPackageBytes)]
    public async Task<IActionResult> Preview(IFormFile? package, CancellationToken cancellationToken)
    {
        if (package is null || package.Length == 0)
        {
            TempData["Error"] = "Hãy chọn một VCMS Content Package.";
            return RedirectToAction(nameof(Index));
        }
        if (package.Length > ContentPackageService.MaximumPackageBytes)
        {
            TempData["Error"] = "Package vượt quá giới hạn 512 MB.";
            return RedirectToAction(nameof(Index));
        }

        var directory = Path.Combine(environment.ContentRootPath, "App_Data", "content-imports");
        Directory.CreateDirectory(directory);
        var token = Guid.NewGuid().ToString("N");
        var path = GetPendingPath(directory, token);
        try
        {
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                await package.CopyToAsync(output, cancellationToken);
            var inspection = await packages.InspectAsync(path, token, cancellationToken);
            return View(inspection);
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or JsonException)
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(string token, CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(token, "N", out _)) return BadRequest();
        var directory = Path.Combine(environment.ContentRootPath, "App_Data", "content-imports");
        var path = GetPendingPath(directory, token);
        if (!System.IO.File.Exists(path))
        {
            TempData["Error"] = "Package chờ import không còn tồn tại. Hãy tải lên lại.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            var backupPath = await packages.ImportAsync(path, cancellationToken);
            TempData["Message"] = $"Đã khôi phục toàn bộ nội dung. Bản sao lưu trước import: {Path.GetFileName(backupPath)}.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Content package import failed");
            TempData["Error"] = $"Không thể import package: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
        finally
        {
            try { System.IO.File.Delete(path); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not delete pending import {Path}", path); }
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel(string token)
    {
        if (Guid.TryParseExact(token, "N", out _))
        {
            var directory = Path.Combine(environment.ContentRootPath, "App_Data", "content-imports");
            var path = GetPendingPath(directory, token);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        return RedirectToAction(nameof(Index));
    }

    private static string GetPendingPath(string directory, string token) => Path.Combine(directory, $"{token}.vcms.zip");
}
