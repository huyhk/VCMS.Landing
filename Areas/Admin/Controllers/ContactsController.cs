using LandingCms.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator")]
public class ContactsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.ContactSubmissions.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync());
    public async Task<IActionResult> Detail(long id) => await db.ContactSubmissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } item ? View(item) : NotFound();
}
