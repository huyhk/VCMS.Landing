using System.ComponentModel.DataAnnotations;

namespace LandingCms.ViewModels;
public class UserEditViewModel
{
    public string? Id { get; set; }
    [Required, StringLength(100)] public string DisplayName { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9._-]+$", ErrorMessage = "Tên đăng nhập chỉ được dùng chữ, số, dấu chấm, gạch dưới và gạch ngang.")]
    public string UserName { get; set; } = "";
    [EmailAddress] public string? Email { get; set; }
    [DataType(DataType.Password), MinLength(10)] public string? Password { get; set; }
    [Required] public string Role { get; set; } = "Editor";
    public bool IsActive { get; set; } = true;
}
