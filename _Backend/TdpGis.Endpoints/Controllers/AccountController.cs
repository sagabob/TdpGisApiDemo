using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TdpGis.Endpoints.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        var redirectUri = Url.Action(nameof(HomeController.Configuration), "Home");
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            redirectUri = returnUrl;

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        var callbackUrl = Url.Action(nameof(HomeController.Index), "Home");
        return SignOut(
            new AuthenticationProperties { RedirectUri = callbackUrl },
            OpenIdConnectDefaults.AuthenticationScheme,
            "Cookies");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

        return RedirectToAction(nameof(HomeController.Configuration), "Home");
    }
}