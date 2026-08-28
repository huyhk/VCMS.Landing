using Microsoft.AspNetCore.Identity;

namespace LandingCms.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
