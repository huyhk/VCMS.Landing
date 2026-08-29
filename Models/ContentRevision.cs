using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class ContentRevision
{
    public long Id { get; set; }
    [Required, StringLength(50)] public string EntityType { get; set; } = "";
    [Required, StringLength(180)] public string EntityKey { get; set; } = "";
    [Required, StringLength(50)] public string Action { get; set; } = "Saved";
    [StringLength(200)] public string? DisplayName { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? CreatedById { get; set; }
    [StringLength(150)] public string? CreatedByName { get; set; }
}
