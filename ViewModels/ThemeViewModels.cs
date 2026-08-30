using LandingCms.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace LandingCms.ViewModels;

public sealed record ThemeListItemViewModel(
    ThemeDefinition Theme,
    bool IsActive,
    IReadOnlyDictionary<string, string> Tokens);

public sealed class ThemeEditorViewModel
{
    public int Id { get; set; }
    public int? BaseThemeId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên theme."), StringLength(150)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }

    [ThemeColor] public string Brand { get; set; } = "#2563eb";
    [ThemeColor] public string BrandHover { get; set; } = "#1d4ed8";
    [ThemeColor] public string BrandContrast { get; set; } = "#ffffff";
    [ThemeColor] public string PageBackground { get; set; } = "#ffffff";
    [ThemeColor] public string Surface { get; set; } = "#ffffff";
    [ThemeColor] public string SurfaceAlt { get; set; } = "#f3f7ff";
    [ThemeColor] public string Text { get; set; } = "#101828";
    [ThemeColor] public string TextMuted { get; set; } = "#667085";
    [ThemeColor] public string Border { get; set; } = "#dbe4f0";
    [ThemeColor] public string HeaderBackground { get; set; } = "#ffffff";
    [ThemeColor] public string HeaderText { get; set; } = "#172033";
    [ThemeColor] public string HeroBackground { get; set; } = "#eef4ff";
    [ThemeColor] public string HeroText { get; set; } = "#101828";
    [ThemeColor] public string HeroMuted { get; set; } = "#526176";
    [ThemeColor] public string ContrastBackground { get; set; } = "#111827";
    [ThemeColor] public string ContrastText { get; set; } = "#ffffff";
    [ThemeColor] public string FooterBackground { get; set; } = "#0b1220";
    [ThemeColor] public string FooterText { get; set; } = "#ffffff";
    [ThemeColor] public string Highlight { get; set; } = "#dbeafe";

    [RegularExpression("^(sharp|soft|rounded)$")] public string CornerStyle { get; set; } = "soft";
    [RegularExpression("^(square|soft|pill)$")] public string ButtonStyle { get; set; } = "soft";
    [RegularExpression("^(none|soft|strong)$")] public string ShadowStyle { get; set; } = "soft";

    public Dictionary<string, string> ToTokens() => new(StringComparer.Ordinal)
    {
        ["brand"] = Brand, ["brandHover"] = BrandHover, ["brandContrast"] = BrandContrast,
        ["pageBackground"] = PageBackground, ["surface"] = Surface, ["surfaceAlt"] = SurfaceAlt,
        ["text"] = Text, ["textMuted"] = TextMuted, ["border"] = Border,
        ["headerBackground"] = HeaderBackground, ["headerText"] = HeaderText,
        ["heroBackground"] = HeroBackground, ["heroText"] = HeroText, ["heroMuted"] = HeroMuted,
        ["contrastBackground"] = ContrastBackground, ["contrastText"] = ContrastText,
        ["footerBackground"] = FooterBackground, ["footerText"] = FooterText, ["highlight"] = Highlight,
        ["cornerStyle"] = CornerStyle, ["buttonStyle"] = ButtonStyle, ["shadowStyle"] = ShadowStyle
    };

    public static ThemeEditorViewModel From(ThemeDefinition theme)
    {
        Dictionary<string, string> tokens;
        try { tokens = JsonSerializer.Deserialize<Dictionary<string, string>>(theme.TokensJson) ?? new(); }
        catch (JsonException) { tokens = new(); }
        string Color(string key, string fallback) => NormalizeColor(tokens.GetValueOrDefault(key), fallback);
        string Choice(string key, string fallback, params string[] allowed) =>
            tokens.TryGetValue(key, out var value) && allowed.Contains(value, StringComparer.Ordinal) ? value : fallback;
        return new ThemeEditorViewModel
        {
            Id = theme.Id, BaseThemeId = theme.BaseThemeId ?? theme.Id, Name = theme.Name, Description = theme.Description,
            Brand = Color("brand", "#2563eb"), BrandHover = Color("brandHover", "#1d4ed8"), BrandContrast = Color("brandContrast", "#ffffff"),
            PageBackground = Color("pageBackground", "#ffffff"), Surface = Color("surface", "#ffffff"), SurfaceAlt = Color("surfaceAlt", "#f3f7ff"),
            Text = Color("text", "#101828"), TextMuted = Color("textMuted", "#667085"), Border = Color("border", "#dbe4f0"),
            HeaderBackground = Color("headerBackground", "#ffffff"), HeaderText = Color("headerText", "#172033"),
            HeroBackground = Color("heroBackground", "#eef4ff"), HeroText = Color("heroText", "#101828"), HeroMuted = Color("heroMuted", "#526176"),
            ContrastBackground = Color("contrastBackground", "#111827"), ContrastText = Color("contrastText", "#ffffff"),
            FooterBackground = Color("footerBackground", "#0b1220"), FooterText = Color("footerText", "#ffffff"), Highlight = Color("highlight", "#dbeafe"),
            CornerStyle = Choice("cornerStyle", "soft", "sharp", "soft", "rounded"),
            ButtonStyle = Choice("buttonStyle", "soft", "square", "soft", "pill"),
            ShadowStyle = Choice("shadowStyle", "soft", "none", "soft", "strong")
        };
    }

    private static string NormalizeColor(string? value, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(value) && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$")) return value;
        if (value?.StartsWith("rgb", StringComparison.OrdinalIgnoreCase) == true)
        {
            var parts = System.Text.RegularExpressions.Regex.Matches(value, "[0-9]+").Select(x => int.Parse(x.Value)).Take(3).ToArray();
            if (parts.Length == 3 && parts.All(x => x is >= 0 and <= 255)) return $"#{parts[0]:X2}{parts[1]:X2}{parts[2]:X2}";
        }
        return fallback;
    }
}

public sealed class ThemeColorAttribute() : RegularExpressionAttribute("^#[0-9a-fA-F]{6}$")
{
    public override string FormatErrorMessage(string name) => "Màu sắc không hợp lệ.";
}
