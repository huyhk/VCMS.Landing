using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Administrator")]
public class UsersController(UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index() => View(await users.Users.OrderBy(x => x.DisplayName).ToListAsync());
    public IActionResult Create() => View("Edit", new UserEditViewModel { IsActive = true });
    public async Task<IActionResult> Edit(string id)
    {
        var user = await users.FindByIdAsync(id); if (user is null) return NotFound();
        return View(new UserEditViewModel { Id = id, DisplayName = user.DisplayName, Email = user.Email!, Role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? DbInitializer.Editor, IsActive = user.IsActive });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UserEditViewModel model)
    {
        if (!new[] { DbInitializer.Administrator, DbInitializer.Editor }.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");
        if (!ModelState.IsValid) return View("Edit", model);
        ApplicationUser user;
        IdentityResult result;
        if (string.IsNullOrEmpty(model.Id))
        {
            if (string.IsNullOrWhiteSpace(model.Password)) { ModelState.AddModelError(nameof(model.Password), "Mật khẩu là bắt buộc."); return View("Edit", model); }
            user = new ApplicationUser { UserName = model.Email, Email = model.Email, DisplayName = model.DisplayName, EmailConfirmed = true, IsActive = model.IsActive };
            result = await users.CreateAsync(user, model.Password);
        }
        else
        {
            user = await users.FindByIdAsync(model.Id) ?? throw new InvalidOperationException();
            user.UserName = user.Email = model.Email; user.DisplayName = model.DisplayName; user.IsActive = model.IsActive;
            result = await users.UpdateAsync(user);
            if (result.Succeeded && !string.IsNullOrWhiteSpace(model.Password)) { var token = await users.GeneratePasswordResetTokenAsync(user); result = await users.ResetPasswordAsync(user, token, model.Password); }
        }
        if (!result.Succeeded) { foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); return View("Edit", model); }
        var oldRoles = await users.GetRolesAsync(user); await users.RemoveFromRolesAsync(user, oldRoles); await users.AddToRoleAsync(user, model.Role);
        return RedirectToAction(nameof(Index));
    }
}
