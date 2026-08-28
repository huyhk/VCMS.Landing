using System.ComponentModel.DataAnnotations;

namespace LandingCms.Models;

public class ContactSubmission
{
    public long Id { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, StringLength(200)] public string Email { get; set; } = "";
    [StringLength(50)] public string? Phone { get; set; }
    [Required, StringLength(4000)] public string Message { get; set; } = "";
    [StringLength(50)] public string Status { get; set; } = "Pending";
    [StringLength(1000)] public string? ErrorMessage { get; set; }
    [StringLength(100)] public string? IpAddress { get; set; }
    [StringLength(500)] public string? UserAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
}
