using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class PopupCampaign
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string InternalName { get; set; } = "";
    public bool IsEnabled { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public int Priority { get; set; }
    public int TriggerDelaySeconds { get; set; } = 2;
    [Required, StringLength(20)] public string Frequency { get; set; } = "session";
    public bool IsDismissible { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<PopupCampaignTranslation> Translations { get; set; } = new List<PopupCampaignTranslation>();
}

public class PopupCampaignTranslation
{
    public int PopupCampaignId { get; set; }
    public PopupCampaign PopupCampaign { get; set; } = null!;
    [StringLength(10)] public string LanguageCode { get; set; } = "";
    public ContentLanguage Language { get; set; } = null!;
    [StringLength(200)] public string? Title { get; set; }
    public string? ContentHtml { get; set; }
    [StringLength(80)] public string? ButtonText { get; set; }
    [StringLength(500)] public string? ButtonUrl { get; set; }
    public long? DesktopMediaId { get; set; }
    public MediaAsset? DesktopMedia { get; set; }
    public long? MobileMediaId { get; set; }
    public MediaAsset? MobileMedia { get; set; }
}
