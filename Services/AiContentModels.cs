using System.ComponentModel.DataAnnotations;

namespace LandingCms.Services;

public sealed class AiLandingBrief
{
    [Required, StringLength(200)] public string Topic { get; set; } = "";
    [StringLength(160)] public string? BrandName { get; set; }
    [StringLength(3000)] public string? ProductOrService { get; set; }
    [StringLength(1000)] public string? TargetAudience { get; set; }
    [StringLength(2000)] public string? Differentiators { get; set; }
    [StringLength(500)] public string? ServiceArea { get; set; }
    [StringLength(1000)] public string? ConversionGoal { get; set; }
    [StringLength(40)] public string Tone { get; set; } = "Chuyên nghiệp, rõ ràng";
    [StringLength(3000)] public string? AdditionalInstructions { get; set; }
}

public sealed class AiTemplateSectionSpec
{
    public string SectionKey { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string SectionType { get; init; } = "";
    public IReadOnlyDictionary<string, SectionFieldSchema> Fields { get; init; } = new Dictionary<string, SectionFieldSchema>();
    public IReadOnlyDictionary<string, SectionFieldSchema> ItemFields { get; init; } = new Dictionary<string, SectionFieldSchema>();
}

public sealed class AiContentRequest
{
    public AiLandingBrief Brief { get; init; } = new();
    public string LanguageCode { get; init; } = "vi";
    public string LanguageName { get; init; } = "Tiếng Việt";
    public IReadOnlyList<AiTemplateSectionSpec> Sections { get; init; } = [];
}

public sealed class AiLandingDraft
{
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = "";
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = "";
    public string LanguageCode { get; set; } = "vi";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<AiSectionDraft> Sections { get; set; } = [];
}

public sealed class AiSectionDraft
{
    public string SectionKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string SectionType { get; set; } = "";
    public Dictionary<string, string?> Content { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Dictionary<string, string?>> Items { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}

public interface IAiContentService
{
    bool IsAvailable { get; }
    string ModelName { get; }
    Task<AiLandingDraft> GenerateLandingAsync(AiContentRequest request, CancellationToken cancellationToken);
}

public interface IAiDraftStore
{
    void Set(AiLandingDraft draft);
    AiLandingDraft? Get(string token, string userId);
    void Remove(string token, string userId);
}
