using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LandingCms.Services;

public interface ILicenseValidationService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
}

public sealed class LicenseValidationService(
    IHttpClientFactory httpClientFactory,
    IOptions<LicensingOptions> optionsAccessor,
    ILicenseState state,
    IWebHostEnvironment environment,
    ILogger<LicenseValidationService> logger) : ILicenseValidationService
{
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool initialized;
    private LicensingOptions Options => optionsAccessor.Value;
    private string CachePath => Path.Combine(environment.ContentRootPath, "App_Data", "license-cache.json");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (initialized) return;
        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (initialized) return;
            if (environment.IsDevelopment() && Options.BypassInDevelopment)
            {
                state.Set(new(LocalLicenseStatus.DevelopmentBypass, "DevelopmentBypass", null, [], DateTime.UtcNow, null,
                    "Đã bỏ qua kiểm tra license trong môi trường Development."));
                initialized = true;
                return;
            }
            await LoadCacheAsync(cancellationToken);
            await RefreshAsync(cancellationToken);
            initialized = true;
        }
        finally { initializationLock.Release(); }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (environment.IsDevelopment() && Options.BypassInDevelopment) return;
        if (!HasRequiredConfiguration())
        {
            state.Set(new(LocalLicenseStatus.Invalid, "NotConfigured", null, [], null, null,
                "Chưa cấu hình License Server, LicenseKey hoặc CanonicalHost."));
            return;
        }
        try
        {
            var client = httpClientFactory.CreateClient("licensing");
            var request = new LicenseValidationRequest(
                Options.ProductCode, Options.LicenseKey, Options.CanonicalHost,
                Assembly.GetExecutingAssembly().GetName().Version?.ToString(), null);
            using var response = await client.PostAsJsonAsync("api/v1/licenses/validate", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<LicenseValidationResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("License Server trả về nội dung rỗng.");
            var snapshot = new LicenseSnapshot(
                result.Valid ? LocalLicenseStatus.Valid : LocalLicenseStatus.Invalid,
                result.Status, result.CanonicalUrl, result.AllowedHosts ?? [], result.CheckedAtUtc,
                result.RefreshAfterUtc, result.Message);
            state.Set(snapshot);
            await SaveCacheAsync(snapshot, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            ApplyOfflinePolicy(ex.Message);
            logger.LogWarning(ex, "Không thể kết nối VNS License Server.");
        }
    }

    private bool HasRequiredConfiguration() => Uri.TryCreate(Options.ServerUrl, UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(Options.LicenseKey) && !string.IsNullOrWhiteSpace(Options.CanonicalHost);

    private async Task LoadCacheAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(CachePath)) return;
        try
        {
            await using var stream = File.OpenRead(CachePath);
            var cached = await JsonSerializer.DeserializeAsync<LicenseSnapshot>(stream, cancellationToken: cancellationToken);
            if (cached is not null) state.Set(cached);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogWarning(ex, "Không thể đọc license cache.");
        }
    }

    private async Task SaveCacheAsync(LicenseSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
            var temporaryPath = CachePath + ".tmp";
            await using (var stream = File.Create(temporaryPath))
                await JsonSerializer.SerializeAsync(stream, snapshot, cancellationToken: cancellationToken);
            File.Move(temporaryPath, CachePath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Không thể ghi license cache.");
        }
    }

    private void ApplyOfflinePolicy(string error)
    {
        var current = state.Current;
        var graceLimit = current.LastOnlineCheckUtc?.AddHours(Math.Max(1, Options.GracePeriodHours));
        if (current.ServerStatus == "Valid" && graceLimit > DateTime.UtcNow)
        {
            state.Set(current with { LocalStatus = LocalLicenseStatus.GracePeriod, NextCheckUtc = DateTime.UtcNow.AddHours(1), Message = "License Server tạm thời không truy cập được; đang dùng thời gian gia hạn." });
            return;
        }
        state.Set(current with { LocalStatus = LocalLicenseStatus.Unavailable, NextCheckUtc = DateTime.UtcNow.AddHours(1), Message = error });
    }

    private sealed record LicenseValidationRequest(string ProductCode, string LicenseKey, string Host, string? ApplicationVersion, string? InstanceId);
    private sealed record LicenseValidationResponse(bool Valid, string Status, string? CanonicalUrl, string[]? AllowedHosts, DateTime CheckedAtUtc, DateTime RefreshAfterUtc, string? Message);
}
