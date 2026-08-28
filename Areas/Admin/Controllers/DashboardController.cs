using LandingCms.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator,Editor")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.LandingSections.OrderBy(x => x.SortOrder).ToListAsync());
}
