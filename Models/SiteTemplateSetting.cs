namespace LandingCms.Models;

public class SiteTemplateSetting
{
    public int Id { get; set; }
    public int ActiveTemplateId { get; set; }
    public PageTemplate ActiveTemplate { get; set; } = null!;
    public int? DraftTemplateId { get; set; }
    public PageTemplate? DraftTemplate { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
