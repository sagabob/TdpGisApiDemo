using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TdpGis.Endpoints.Models;
using TdpGis.Endpoints.Options;

namespace TdpGis.Endpoints.Controllers;

public class AccountController(IOptions<AdminDashboardOptions> options) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToLocal(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var opts = options.Value;
        if (string.IsNullOrEmpty(opts.Password))
            ModelState.AddModelError(string.Empty,
                "Admin sign-in is not configured. Set AdminDashboard:Password (and optionally AdminDashboard:User) in appsettings, user secrets, or environment variables.");

        if (!ModelState.IsValid) return View(model);

        var nameOk = string.Equals(model.UserName.Trim(), opts.User.Trim(), StringComparison.Ordinal);
        var passOk = string.Equals(model.Password, opts.Password, StringComparison.Ordinal);
        if (!nameOk || !passOk || string.IsNullOrEmpty(opts.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid user name or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, model.UserName.Trim()),
            new(ClaimTypes.NameIdentifier, model.UserName.Trim())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

        return RedirectToAction(nameof(HomeController.Configuration), "Home");
    }
}