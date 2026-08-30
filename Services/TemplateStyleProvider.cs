using System.Collections.Concurrent;

namespace LandingCms.Services;

public interface ITemplateStyleProvider
{
    string? GetInlineCss(string virtualPath);
}

public sealed class TemplateStyleProvider(
    IWebHostEnvironment environment,
    ILogger<TemplateStyleProvider> logger) : ITemplateStyleProvider
{
    private static readonly IReadOnlyDictionary<string, string> AllowedStylesheets =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["~/css/bundles/corporate.bundle.css"] = "corporate.bundle.css",
            ["~/css/bundles/minimal.bundle.css"] = "minimal.bundle.css",
            ["~/css/bundles/editorial.bundle.css"] = "editorial.bundle.css"
        };

    private readonly ConcurrentDictionary<string, string> cache = new(StringComparer.Ordinal);

    public string? GetInlineCss(string virtualPath)
    {
        if (!AllowedStylesheets.TryGetValue(virtualPath, out var fileName))
        {
            return null;
        }

        if (cache.TryGetValue(virtualPath, out var cachedCss))
        {
            return cachedCss;
        }

        var filePath = Path.Combine(environment.WebRootPath, "css", "bundles", fileName);
        try
        {
            var css = File.ReadAllText(filePath);
            return cache.GetOrAdd(virtualPath, css);
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not load template stylesheet {StylesheetPath} for inlining.", filePath);
            return null;
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "Could not access template stylesheet {StylesheetPath} for inlining.", filePath);
            return null;
        }
    }
}
