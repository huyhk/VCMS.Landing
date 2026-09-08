using System.ComponentModel.DataAnnotations;
using LandingCms.Models;

namespace LandingCms.ViewModels;

public sealed class PopupCampaignEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string InternalName { get; set; } = "";
    public bool IsEnabled { get; set; }
    [DataType(DataType.DateTime)] public DateTime? StartAtLocal { get; set; }
    [DataType(DataType.DateTime)] public DateTime? EndAtLocal { get; set; }
    [Range(-1000, 1000)] public int Priority { get; set; }
    [Range(0, 60)] public int TriggerDelaySeconds { get; set; } = 2;
    [Required] public string Frequency { get; set; } = "session";
    public bool IsDismissible { get; set; } = true;
    [Required, StringLength(10)] public string LanguageCode { get; set; } = "vi";
    [StringLength(200)] public string? Title { get; set; }
    public string? ContentHtml { get; set; }
    [StringLength(80)] public string? ButtonText { get; set; }
    [StringLength(500)] public string? ButtonUrl { get; set; }
    public long? DesktopMediaId { get; set; }
    public long? MobileMediaId { get; set; }
    public IReadOnlyList<ContentLanguage> Languages { get; set; } = [];
    public IReadOnlyList<MediaAsset> Media { get; set; } = [];
    public bool HasTranslation { get; set; }
}

public sealed record PopupRenderViewModel(
    int Id, int Version, string LanguageCode, string? Title, string? ContentHtml,
    string? ButtonText, string? ButtonUrl, MediaAsset? DesktopMedia, MediaAsset? MobileMedia,
    int TriggerDelaySeconds, string Frequency, bool IsDismissible);
