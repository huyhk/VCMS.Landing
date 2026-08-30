using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LandingCms.Services;

public interface IThemeCssService
{
    IReadOnlyDictionary<string, string> GetTokens(string tokensJson);
    string BuildCss(string tokensJson);
}

public sealed partial class ThemeCssService : IThemeCssService
{
    private static readonly IReadOnlyDictionary<string, string> TokenVariables =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["brand"] = "--brand", ["brandHover"] = "--brand-hover", ["brandContrast"] = "--brand-contrast",
            ["pageBackground"] = "--page-bg", ["surface"] = "--surface", ["surfaceAlt"] = "--surface-alt",
            ["text"] = "--ink", ["textMuted"] = "--muted", ["border"] = "--line",
            ["headerBackground"] = "--header-bg", ["headerText"] = "--header-text",
            ["heroBackground"] = "--hero-bg", ["heroText"] = "--hero-text", ["heroMuted"] = "--hero-muted",
            ["contrastBackground"] = "--contrast-bg", ["contrastText"] = "--contrast-text",
            ["footerBackground"] = "--footer-bg", ["footerText"] = "--footer-text", ["highlight"] = "--highlight"
        };

    public IReadOnlyDictionary<string, string> GetTokens(string tokensJson)
    {
        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(tokensJson) ?? new();
            return values.Where(x => TokenVariables.ContainsKey(x.Key) && IsSafeColor(x.Value))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    public string BuildCss(string tokensJson)
    {
        var tokens = GetTokens(tokensJson);
        var css = new StringBuilder(1400).Append(":root{");
        foreach (var token in tokens)
            css.Append(TokenVariables[token.Key]).Append(':').Append(token.Value).Append(';');
        css.Append('}');
        css.Append("body{color:var(--ink);background:var(--page-bg)}");
        css.Append(".site-header{color:var(--header-text);background:var(--header-bg);border-bottom-color:var(--line)}");
        css.Append(".nav nav a,.menu-button{color:var(--header-text)}");
        css.Append(".btn{color:var(--brand-contrast);background:var(--brand)}.btn:hover{background:var(--brand-hover)}");
        css.Append(".hero:not(.has-background){color:var(--hero-text);background:var(--hero-bg)}");
        css.Append(".hero:not(.has-background) .lead{color:var(--hero-muted)}");
        css.Append(".section{background:var(--page-bg)}.section-soft,.contact-section{background:var(--surface-alt)}");
        css.Append(".service-card,.testimonial-card,.media-card,.pricing-card,.team-card,.partner-logo,.faq-list details,.contact-form{color:var(--ink);background:var(--surface);border-color:var(--line)}");
        css.Append(".stats{color:var(--contrast-text);background:var(--contrast-bg)}.stats h2,.stats-grid strong{color:var(--contrast-text)}");
        css.Append(".stats-grid span{color:color-mix(in srgb,var(--contrast-text) 72%,transparent)}");
        css.Append(".cta{color:var(--brand-contrast);background:var(--brand)}");
        css.Append("footer{color:var(--footer-text);background:var(--footer-bg)}footer p,footer span,footer a:not(.brand){color:color-mix(in srgb,var(--footer-text) 68%,transparent)}");
        return css.ToString();
    }

    private static bool IsSafeColor(string value) => value.Length <= 64 && ColorValueRegex().IsMatch(value);

    [GeneratedRegex(@"^(#[0-9a-fA-F]{3,8}|rgba?\([0-9., %]+\)|hsla?\([0-9., %]+\))$")]
    private static partial Regex ColorValueRegex();
}
