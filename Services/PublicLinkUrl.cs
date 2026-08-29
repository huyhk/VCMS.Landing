namespace LandingCms.Services;

public static class PublicLinkUrl
{
    public static string? Normalize(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.StartsWith('#')) return value;
        if (value.StartsWith('/') && !value.StartsWith("//")) return value;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        return uri.Scheme is Uri.UriSchemeHttp or Uri.UriSchemeHttps or "mailto" or "tel" ? value : null;
    }
}
