using System.Text.RegularExpressions;

namespace LandingCms.Services;

public static partial class MediaEmbedUrl
{
    public static bool TryResolve(string? value, out string embedUrl)
    {
        embedUrl = "";
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return false;

        var host = uri.Host.ToLowerInvariant();
        if (host is "youtu.be" or "www.youtu.be")
            return TryYouTubeId(uri.AbsolutePath.Trim('/').Split('/').FirstOrDefault(), out embedUrl);

        if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com" or "youtube-nocookie.com" or "www.youtube-nocookie.com")
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var videoId = segments.Length >= 2 && segments[0] is "embed" or "shorts"
                ? segments[1]
                : GetQueryValue(uri.Query, "v");
            return TryYouTubeId(videoId, out embedUrl);
        }

        if (host is "vimeo.com" or "www.vimeo.com" or "player.vimeo.com")
        {
            var videoId = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault(x => VimeoIdPattern().IsMatch(x));
            if (videoId is not null)
            {
                embedUrl = $"https://player.vimeo.com/video/{videoId}";
                return true;
            }
        }

        return false;
    }

    private static bool TryYouTubeId(string? videoId, out string embedUrl)
    {
        embedUrl = "";
        if (string.IsNullOrWhiteSpace(videoId) || !YouTubeIdPattern().IsMatch(videoId)) return false;
        embedUrl = $"https://www.youtube-nocookie.com/embed/{videoId}";
        return true;
    }

    private static string? GetQueryValue(string query, string key)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(parts[1]);
        }
        return null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{6,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex YouTubeIdPattern();

    [GeneratedRegex("^[0-9]{5,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex VimeoIdPattern();
}
