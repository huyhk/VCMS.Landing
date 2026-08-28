using System.ComponentModel.DataAnnotations;

namespace LandingCms.ViewModels;
public class LoginViewModel
{
    [Required, StringLength(100)] public string UserName { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
