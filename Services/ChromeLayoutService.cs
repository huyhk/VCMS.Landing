using System.Text.Json;

namespace LandingCms.Services;

public sealed class ChromeLayout
{
    public string Behavior { get; set; } = "sticky";
    public List<ChromeRow> Rows { get; set; } = [];
}

public sealed class ChromeRow
{
    public string Key { get; set; } = "row";
    public string Container { get; set; } = "boxed";
    public string Background { get; set; } = "surface";
    public string Height { get; set; } = "standard";
    public long? BackgroundMediaId { get; set; }
    public long? MobileBackgroundMediaId { get; set; }
    public string BackgroundSize { get; set; } = "cover";
    public string BackgroundPosition { get; set; } = "center";
    public string OverlayColor { get; set; } = "#000000";
    public int OverlayOpacity { get; set; }
    public bool HideOnMobile { get; set; }
    public List<ChromeColumn> Columns { get; set; } = [];
}

public sealed class ChromeColumn
{
    public string Width { get; set; } = "fill";
    public string Align { get; set; } = "left";
    public List<ChromeComponent> Components { get; set; } = [];
}

public sealed class ChromeComponent
{
    public string Type { get; set; } = "text";
    public string? Variant { get; set; }
    public string? Text { get; set; }
    public string? Url { get; set; }
    public string FontWeight { get; set; } = "default";
    public bool Italic { get; set; }
    public string FontSize { get; set; } = "default";
    public string? TextColor { get; set; }
    public string TextAlign { get; set; } = "default";
    public string SpacingTop { get; set; } = "none";
    public string SpacingBottom { get; set; } = "none";
    public Dictionary<string, string> Translations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool HideOnMobile { get; set; }
}

public interface IChromeLayoutService
{
    ChromeLayout ParseHeader(string? json);
    ChromeLayout ParseFooter(string? json);
    string NormalizeHeader(string? json);
    string NormalizeFooter(string? json);
    string GetHeaderPreset(string key);
    string GetFooterPreset(string key);
}

public sealed class ChromeLayoutService : IChromeLayoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly HashSet<string> ComponentTypes = new(StringComparer.Ordinal)
        { "logo", "navigation", "button", "language", "phone", "email", "address", "social", "company", "copyright", "text" };
    private static readonly HashSet<string> Backgrounds = new(StringComparer.Ordinal) { "surface", "brand", "contrast", "transparent", "image" };
    private static readonly HashSet<string> Heights = new(StringComparer.Ordinal) { "compact", "standard", "large" };
    private static readonly HashSet<string> Widths = new(StringComparer.Ordinal) { "auto", "fill", "1", "2", "3", "4" };
    private static readonly HashSet<string> Alignments = new(StringComparer.Ordinal) { "left", "center", "right" };
    private static readonly HashSet<string> FontWeights = new(StringComparer.Ordinal) { "default", "normal", "bold" };
    private static readonly HashSet<string> FontSizes = new(StringComparer.Ordinal) { "default", "small", "large" };
    private static readonly HashSet<string> TextAlignments = new(StringComparer.Ordinal) { "default", "left", "center", "right" };
    private static readonly HashSet<string> Spacings = new(StringComparer.Ordinal) { "none", "small", "medium", "large" };

    public ChromeLayout ParseHeader(string? json) => Parse(json, Header("standard"));
    public ChromeLayout ParseFooter(string? json) => Parse(json, Footer("corporate"));
    public string NormalizeHeader(string? json) => Serialize(Validate(ParseRequired(json), true));
    public string NormalizeFooter(string? json) => Serialize(Validate(ParseRequired(json), false));
    public string GetHeaderPreset(string key) => Serialize(Header(key));
    public string GetFooterPreset(string key) => Serialize(Footer(key));

    private static ChromeLayout Parse(string? json, ChromeLayout fallback)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return fallback;
        try { return Validate(JsonSerializer.Deserialize<ChromeLayout>(json, JsonOptions) ?? fallback, false); }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return fallback; }
    }

    private static ChromeLayout ParseRequired(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("Cấu hình không được để trống.");
        try { return JsonSerializer.Deserialize<ChromeLayout>(json, JsonOptions) ?? throw new InvalidOperationException("Cấu hình không hợp lệ."); }
        catch (JsonException ex) { throw new InvalidOperationException("Cấu hình JSON không hợp lệ.", ex); }
    }

    private static ChromeLayout Validate(ChromeLayout layout, bool requireNavigation)
    {
        if (layout.Rows.Count is < 1 or > 5) throw new InvalidOperationException("Header/Footer phải có từ 1 đến 5 hàng.");
        layout.Behavior = layout.Behavior is "sticky" or "static" ? layout.Behavior : "static";
        var componentCount = 0;
        foreach (var row in layout.Rows)
        {
            row.Key = Slug(row.Key, "row"); row.Container = row.Container == "full" ? "full" : "boxed";
            row.Background = Backgrounds.Contains(row.Background) ? row.Background : "surface";
            row.Height = Heights.Contains(row.Height) ? row.Height : "standard";
            row.BackgroundMediaId = row.BackgroundMediaId > 0 ? row.BackgroundMediaId : null;
            row.MobileBackgroundMediaId = row.MobileBackgroundMediaId > 0 ? row.MobileBackgroundMediaId : null;
            row.BackgroundSize = row.BackgroundSize is "cover" or "contain" or "auto" ? row.BackgroundSize : "cover";
            row.BackgroundPosition = row.BackgroundPosition is "center" or "left" or "right" or "top" or "bottom" ? row.BackgroundPosition : "center";
            row.OverlayColor = IsHexColor(row.OverlayColor) ? row.OverlayColor.ToLowerInvariant() : "#000000";
            row.OverlayOpacity = Math.Clamp(row.OverlayOpacity, 0, 100);
            if (row.Columns.Count is < 1 or > 4) throw new InvalidOperationException("Mỗi hàng phải có từ 1 đến 4 cột.");
            foreach (var column in row.Columns)
            {
                column.Width = Widths.Contains(column.Width) ? column.Width : "fill";
                column.Align = Alignments.Contains(column.Align) ? column.Align : "left";
                if (column.Components.Count > 10) throw new InvalidOperationException("Mỗi cột tối đa 10 thành phần.");
                foreach (var component in column.Components)
                {
                    if (!ComponentTypes.Contains(component.Type)) throw new InvalidOperationException($"Thành phần '{component.Type}' không được hỗ trợ.");
                    component.Variant = Slug(component.Variant, "default");
                    component.Text = Trim(component.Text, 200); component.Url = SafeUrl(component.Url);
                    component.FontWeight = FontWeights.Contains(component.FontWeight) ? component.FontWeight : "default";
                    component.FontSize = FontSizes.Contains(component.FontSize) ? component.FontSize : "default";
                    component.TextColor = IsHexColor(component.TextColor) ? component.TextColor!.ToLowerInvariant() : null;
                    component.TextAlign = TextAlignments.Contains(component.TextAlign) ? component.TextAlign : "default";
                    component.SpacingTop = Spacings.Contains(component.SpacingTop) ? component.SpacingTop : "none";
                    component.SpacingBottom = Spacings.Contains(component.SpacingBottom) ? component.SpacingBottom : "none";
                    component.Translations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (component.Translations.Count > 20) throw new InvalidOperationException("Mỗi thành phần tối đa 20 bản dịch.");
                    var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var translation in component.Translations)
                    {
                        var languageCode = translation.Key.Trim().ToLowerInvariant();
                        if (languageCode.Length is < 2 or > 10 || languageCode.Any(x => !char.IsLetterOrDigit(x) && x != '-'))
                            throw new InvalidOperationException($"Mã ngôn ngữ '{translation.Key}' không hợp lệ.");
                        var translatedText = Trim(translation.Value, 200);
                        if (translatedText is not null) translations[languageCode] = translatedText;
                    }
                    component.Translations = translations;
                    componentCount++;
                }
            }
        }
        if (componentCount == 0) throw new InvalidOperationException("Cấu hình cần ít nhất một thành phần.");
        if (requireNavigation && !layout.Rows.SelectMany(x => x.Columns).SelectMany(x => x.Components).Any(x => x.Type == "logo"))
            throw new InvalidOperationException("Header cần có thành phần Logo.");
        return layout;
    }

    private static string Serialize(ChromeLayout value) => JsonSerializer.Serialize(value, JsonOptions);
    private static bool IsHexColor(string? value) => value is { Length: 7 } && value[0] == '#' && value[1..].All(Uri.IsHexDigit);
    private static string Slug(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : new string(value.Where(x => char.IsLetterOrDigit(x) || x == '-').Take(40).ToArray()).ToLowerInvariant() is { Length: > 0 } result ? result : fallback;
    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, max)];
    }

    private static string? SafeUrl(string? value)
    {
        var url = Trim(value, 500);
        if (url is null) return null;
        if (url.StartsWith('#') || url.StartsWith('/') ||
            Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https" or "mailto" or "tel")
            return url;
        throw new InvalidOperationException("URL của thành phần không hợp lệ.");
    }

    private static ChromeColumn Col(string width, string align, params string[] types) => new() { Width = width, Align = align, Components = types.Select(x => new ChromeComponent { Type = x }).ToList() };
    private static ChromeRow Row(string key, string background, string height, bool hideMobile, params ChromeColumn[] columns) => new() { Key = key, Background = background, Height = height, HideOnMobile = hideMobile, Columns = columns.ToList() };
    private static ChromeLayout Header(string key) => key switch
    {
        "benka" => new() { Behavior = "sticky", Rows = [Row("topbar", "brand", "compact", true, Col("fill", "right", "phone", "email", "address")), Row("navigation", "surface", "standard", false, Col("auto", "left", "logo"), Col("fill", "right", "navigation", "button", "language"))] },
        "centered" => new() { Behavior = "sticky", Rows = [Row("logo", "surface", "compact", false, Col("fill", "center", "logo")), Row("navigation", "surface", "compact", false, Col("fill", "center", "navigation", "button", "language"))] },
        "minimal" => new() { Behavior = "sticky", Rows = [Row("navigation", "surface", "standard", false, Col("auto", "left", "logo"), Col("fill", "right", "button"))] },
        _ => new() { Behavior = "sticky", Rows = [Row("navigation", "surface", "standard", false, Col("auto", "left", "logo"), Col("fill", "right", "navigation", "button", "language"))] }
    };
    private static ChromeLayout Footer(string key) => key switch
    {
        "minimal" => new() { Rows = [Row("footer", "contrast", "compact", false, Col("fill", "center", "copyright"))] },
        "benka" => new() { Rows = [Row("main", "surface", "large", false, Col("2", "left", "logo", "company", "social"), Col("1", "left", "navigation"), Col("1", "left", "phone", "email", "address")), Row("bottom", "brand", "compact", false, Col("fill", "center", "copyright"))] },
        _ => new() { Rows = [Row("main", "contrast", "large", false, Col("2", "left", "logo", "company", "social"), Col("1", "left", "navigation"), Col("1", "left", "phone", "email", "address")), Row("bottom", "contrast", "compact", false, Col("fill", "center", "copyright"))] }
    };
}
