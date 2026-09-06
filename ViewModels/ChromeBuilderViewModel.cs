namespace LandingCms.ViewModels;
public sealed record ChromeLanguageOption(string Code, string Name, bool IsDefault);
public sealed class ChromeBuilderViewModel
{
    public string TemplateName { get; set; } = "";
    public string HeaderJson { get; set; } = "{}";
    public string FooterJson { get; set; } = "{}";
    public Dictionary<string, string> HeaderPresets { get; set; } = [];
    public Dictionary<string, string> FooterPresets { get; set; } = [];
    public IReadOnlyList<ChromeLanguageOption> Languages { get; set; } = [];
}
