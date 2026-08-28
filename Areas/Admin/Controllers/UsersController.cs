using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator")]
public class UsersController(UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var query = users.Users.AsNoTracking();
        if (!User.IsInRole(DbInitializer.SuperAdministrator))
        {
            var superAdminIds = (await users.GetUsersInRoleAsync(DbInitializer.SuperAdministrator))
                .Select(x => x.Id).ToArray();
            query = query.Where(x => !superAdminIds.Contains(x.Id));
        }
        return View(await query.OrderBy(x => x.DisplayName).ToListAsync());
    }
    public IActionResult Create() => View("Edit", new UserEditViewModel { IsActive = true });
    public async Task<IActionResult> Edit(string id)
    {
        var user = await users.FindByIdAsync(id); if (user is null) return NotFound();
        var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? DbInitializer.Editor;
        if (role == DbInitializer.SuperAdministrator && !User.IsInRole(DbInitializer.SuperAdministrator)) return Forbid();
        return View(new UserEditViewModel { Id = id, DisplayName = user.DisplayName, UserName = user.UserName!, Email = user.Email, Role = role, IsActive = user.IsActive });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UserEditViewModel model)
    {
        var allowedRoles = User.IsInRole(DbInitializer.SuperAdministrator)
            ? new[] { DbInitializer.SuperAdministrator, DbInitializer.Administrator, DbInitializer.Editor }
            : new[] { DbInitializer.Administrator, DbInitializer.Editor };
        if (!allowedRoles.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");
        if (!ModelState.IsValid) return View("Edit", model);
        ApplicationUser user;
        IdentityResult result;
        if (string.IsNullOrEmpty(model.Id))
        {
            if (string.IsNullOrWhiteSpace(model.Password)) { ModelState.AddModelError(nameof(model.Password), "Mật khẩu là bắt buộc."); return View("Edit", model); }
            user = new ApplicationUser { UserName = model.UserName, Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email, DisplayName = model.DisplayName, EmailConfirmed = !string.IsNullOrWhiteSpace(model.Email), IsActive = model.IsActive };
            result = await users.CreateAsync(user, model.Password);
        }
        else
        {
            user = await users.FindByIdAsync(model.Id) ?? throw new InvalidOperationException();
            var currentRoles = await users.GetRolesAsync(user);
            if (currentRoles.Contains(DbInitializer.SuperAdministrator) && !User.IsInRole(DbInitializer.SuperAdministrator)) return Forbid();
            user.UserName = model.UserName;
            user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email;
            user.EmailConfirmed = !string.IsNullOrWhiteSpace(model.Email);
            user.DisplayName = model.DisplayName; user.IsActive = model.IsActive;
            result = await users.UpdateAsync(user);
        }
        if (!result.Succeeded) { foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); return View("Edit", model); }
        var oldRoles = await users.GetRolesAsync(user); await users.RemoveFromRolesAsync(user, oldRoles); await users.AddToRoleAsync(user, model.Role);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var target = await users.FindByIdAsync(id);
        if (target is null) return NotFound();
        if (!await CanResetPasswordAsync(target)) return Forbid();
        return View(new ResetPasswordViewModel { UserId = target.Id, UserName = target.UserName!, DisplayName = target.DisplayName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var target = await users.FindByIdAsync(model.UserId);
        if (target is null) return NotFound();
        if (!await CanResetPasswordAsync(target)) return Forbid();
        model.UserName = target.UserName!; model.DisplayName = target.DisplayName;
        if (!ModelState.IsValid) return View(model);
        var token = await users.GeneratePasswordResetTokenAsync(target);
        var result = await users.ResetPasswordAsync(target, token, model.NewPassword);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(model); }
        TempData["Message"] = $"Đã đặt lại mật khẩu cho {target.UserName}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CanResetPasswordAsync(ApplicationUser target)
    {
        if (target.Id == User.FindFirstValue(ClaimTypes.NameIdentifier)) return false;
        var roles = await users.GetRolesAsync(target);
        if (User.IsInRole(DbInitializer.SuperAdministrator)) return !roles.Contains(DbInitializer.SuperAdministrator);
        return User.IsInRole(DbInitializer.Administrator) && roles.Contains(DbInitializer.Editor);
    }
}
