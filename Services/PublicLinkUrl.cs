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
        return uri.Scheme == Uri.UriSchemeHttp ||
               uri.Scheme == Uri.UriSchemeHttps ||
               uri.Scheme == "mailto" ||
               uri.Scheme == "tel"
            ? value
            : null;
    }
}
