using Ganss.Xss;

namespace LandingCms.Services;

public interface IContentHtmlSanitizer
{
    string Sanitize(string? html);
}

public sealed class ContentHtmlSanitizer : IContentHtmlSanitizer
{
    private readonly HtmlSanitizer sanitizer = new();

    public ContentHtmlSanitizer()
    {
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "br", "strong", "b", "em", "i", "u", "s", "h2", "h3", "h4", "ul", "ol", "li", "blockquote", "a" })
            sanitizer.AllowedTags.Add(tag);
        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "href", "title" })
            sanitizer.AllowedAttributes.Add(attribute);
        sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto", "tel" })
            sanitizer.AllowedSchemes.Add(scheme);
    }

    public string Sanitize(string? html) => string.IsNullOrWhiteSpace(html) ? "" : sanitizer.Sanitize(html);
}
