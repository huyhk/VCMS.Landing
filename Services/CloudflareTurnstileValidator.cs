using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace LandingCms.Services;

public sealed class CloudflareTurnstileOptions
{
    public string SiteKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public bool HasSiteKey => !string.IsNullOrWhiteSpace(SiteKey);
    public bool HasSecretKey => !string.IsNullOrWhiteSpace(SecretKey);
    public bool IsEnabled => HasSiteKey && HasSecretKey;
}

public interface ICloudflareTurnstileValidator
{
    Task<bool> ValidateAsync(string? token, CancellationToken cancellationToken = default);
}

public sealed class CloudflareTurnstileValidator(
    HttpClient httpClient,
    IOptions<CloudflareTurnstileOptions> options,
    ILogger<CloudflareTurnstileValidator> logger) : ICloudflareTurnstileValidator
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<bool> ValidateAsync(string? token, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsEnabled) return true;
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var fields = new Dictionary<string, string>
            {
                ["secret"] = settings.SecretKey,
                ["response"] = token
            };

            using var response = await httpClient.PostAsync(VerifyUrl, new FormUrlEncodedContent(fields), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Cloudflare Turnstile returned HTTP {StatusCode}.", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken: cancellationToken);
            if (result?.Success == true) return true;
            logger.LogWarning("Cloudflare Turnstile rejected a contact request. Errors: {Errors}",
                string.Join(", ", result?.ErrorCodes ?? Array.Empty<string>()));
            return false;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Cloudflare Turnstile validation failed.");
            return false;
        }
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("error-codes")] public string[] ErrorCodes { get; set; } = Array.Empty<string>();
    }
}
