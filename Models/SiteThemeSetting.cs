namespace LandingCms.Models;

public class SiteThemeSetting
{
    public int Id { get; set; }
    public int ActiveThemeId { get; set; }
    public ThemeDefinition ActiveTheme { get; set; } = null!;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
