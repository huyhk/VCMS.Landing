using Ganss.Xss;

namespace LandingCms.Services;

public interface IContentHtmlSanitizer
{
    string Sanitize(string? html, string? policyName);
    IReadOnlySet<string> GetAllowedTags(string? policyName);
}

public sealed class ContentHtmlSanitizer : IContentHtmlSanitizer
{
    private readonly IReadOnlyDictionary<string, HtmlSanitizer> policies;

    public ContentHtmlSanitizer()
    {
        policies = new Dictionary<string, HtmlSanitizer>(StringComparer.OrdinalIgnoreCase)
        {
            ["BasicContent"] = CreateSanitizer("div", "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a"),
            ["RichContent"] = CreateSanitizer("div", "p", "br", "strong", "b", "em", "i", "u", "s", "h2", "h3", "h4", "ul", "ol", "li", "blockquote", "a"),
            ["InlineOnly"] = CreateSanitizer("br", "strong", "b", "em", "i", "u", "a")
        };
    }

    public string Sanitize(string? html, string? policyName)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var sanitizer = policyName is not null && policies.TryGetValue(policyName, out var selected)
            ? selected : policies["BasicContent"];
        return sanitizer.Sanitize(html);
    }

    public IReadOnlySet<string> GetAllowedTags(string? policyName)
    {
        var sanitizer = policyName is not null && policies.TryGetValue(policyName, out var selected)
            ? selected : policies["BasicContent"];
        return sanitizer.AllowedTags.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HtmlSanitizer CreateSanitizer(params string[] allowedTags)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in allowedTags) sanitizer.AllowedTags.Add(tag);
        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "href", "title" })
            sanitizer.AllowedAttributes.Add(attribute);
        sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto", "tel" })
            sanitizer.AllowedSchemes.Add(scheme);
        return sanitizer;
    }
}
