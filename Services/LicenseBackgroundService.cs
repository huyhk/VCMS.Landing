using Microsoft.Extensions.Options;

namespace LandingCms.Services;

public sealed class LicenseBackgroundService(
    ILicenseValidationService validation,
    IOptions<LicensingOptions> options,
    ILogger<LicenseBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await validation.InitializeAsync(stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        catch (Exception ex) { logger.LogError(ex, "Không thể khởi tạo license validation."); }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(Math.Max(1, options.Value.RefreshIntervalHours)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await validation.RefreshAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "License background validation thất bại."); }
        }
    }
}
