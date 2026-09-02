using LandingCms.Data;
using LandingCms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = DbInitializer.SuperAdministrator)]
public sealed class LicensingController(ILicenseState state, ILicenseValidationService validation) : Controller
{
    public IActionResult Index() => View(state.Current);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh()
    {
        await validation.RefreshAsync(HttpContext.RequestAborted);
        TempData["Message"] = "Đã kiểm tra lại trạng thái license.";
        return RedirectToAction(nameof(Index));
    }
}

