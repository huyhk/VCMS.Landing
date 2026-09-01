using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class ContentLanguage
{
    [Key, StringLength(10)] public string Code { get; set; } = "vi";
    [Required, StringLength(80)] public string Name { get; set; } = "Tiếng Việt";
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public ICollection<SectionContentTranslation> SectionContentTranslations { get; set; } = new List<SectionContentTranslation>();
    public ICollection<SectionItemTranslation> SectionItemTranslations { get; set; } = new List<SectionItemTranslation>();
}
