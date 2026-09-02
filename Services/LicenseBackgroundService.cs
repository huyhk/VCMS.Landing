namespace LandingCms.Services;

public sealed class LicenseBackgroundService(
    ILicenseValidationService validation,
    ILicenseState state,
    ILogger<LicenseBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await validation.InitializeAsync(stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        catch (Exception ex) { logger.LogError(ex, "Không thể khởi tạo license validation."); }

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextCheck = state.Current.NextCheckUtc ?? DateTime.UtcNow.AddDays(1);
            var delay = nextCheck - DateTime.UtcNow;
            if (delay < TimeSpan.FromSeconds(5)) delay = TimeSpan.FromSeconds(5);
            await Task.Delay(delay, stoppingToken);
            try { await validation.RefreshAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "License background validation thất bại."); }
        }
    }
}
