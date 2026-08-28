using System.ComponentModel.DataAnnotations;

namespace LandingCms.ViewModels;

public class ContactFormViewModel
{
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [Phone, StringLength(50)] public string? Phone { get; set; }
    [Required, StringLength(4000, MinimumLength = 10)] public string Message { get; set; } = "";
    [StringLength(200)] public string? Website { get; set; }
}
