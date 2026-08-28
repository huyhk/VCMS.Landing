using System.ComponentModel.DataAnnotations;

namespace LandingCms.ViewModels;
public class UserEditViewModel
{
    public string? Id { get; set; }
    [Required, StringLength(100)] public string DisplayName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [DataType(DataType.Password), MinLength(10)] public string? Password { get; set; }
    [Required] public string Role { get; set; } = "Editor";
    public bool IsActive { get; set; } = true;
}
