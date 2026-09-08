using LandingCms.Services;

namespace LandingCms.Tests;

public sealed class PublicInputSafetyTests
{
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example/path")]
    [InlineData("data:text/html,test")]
    public void Public_links_reject_unsafe_urls(string value) =>
        Assert.Null(PublicLinkUrl.Normalize(value));

    [Theory]
    [InlineData("#contact")]
    [InlineData("/en/")]
    [InlineData("https://example.com/path")]
    [InlineData("mailto:hello@example.com")]
    public void Public_links_accept_supported_urls(string value) =>
        Assert.Equal(value, PublicLinkUrl.Normalize(value));

    [Fact]
    public void Rich_content_removes_scripts_and_event_handlers()
    {
        var sanitizer = new ContentHtmlSanitizer();
        var result = sanitizer.Sanitize("<p onclick=\"bad()\">Safe</p><script>alert(1)</script>", "RichContent");
        Assert.Contains("<p>Safe</p>", result);
        Assert.False(result.Contains("onclick", StringComparison.OrdinalIgnoreCase));
        Assert.False(result.Contains("<script", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/123456789", "https://player.vimeo.com/video/123456789")]
    public void Media_urls_resolve_only_to_supported_embed_hosts(string input, string expected)
    {
        Assert.True(MediaEmbedUrl.TryResolve(input, out var actual));
        Assert.Equal(expected, actual);
    }
}
