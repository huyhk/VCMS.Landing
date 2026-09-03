using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator,Editor")]
public sealed class HelpController : Controller
{
    public IActionResult Index() => View();
}
