using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LandingCms.Controllers;
[Route("account")]
public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : Controller
{
    [AllowAnonymous, HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, ValidateAntiForgeryToken, HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "Thông tin đăng nhập không hợp lệ.");
            return View(model);
        }
        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/admin");
        ModelState.AddModelError("", "Thông tin đăng nhập không hợp lệ.");
        return View(model);
    }

    [Authorize, ValidateAntiForgeryToken, HttpPost("logout")]
    public async Task<IActionResult> Logout() { await signInManager.SignOutAsync(); return RedirectToAction("Index", "Home"); }

    [AllowAnonymous, HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();
}
