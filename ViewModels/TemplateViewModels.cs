using LandingCms.Models;

namespace LandingCms.ViewModels;

public record TemplateListItemViewModel(PageTemplate Template, bool IsActive, bool IsDraft, int SectionCount);
public record DashboardViewModel(string TemplateName, IReadOnlyList<SectionListItemViewModel> Sections);
