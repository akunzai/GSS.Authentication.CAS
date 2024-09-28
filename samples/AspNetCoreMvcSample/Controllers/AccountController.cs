using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreMvcSample.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    // GET /Account/Login
    [HttpGet]
    public ActionResult Login(string scheme)
    {
        if (string.IsNullOrWhiteSpace(scheme))
        {
            return View();
        }

        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, scheme);
    }

    // GET /Account/Logout
    [HttpGet]
    public async Task Logout(string redirectUrl)
    {
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            redirectUrl = "/";
        }
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        await HttpContext.SignOutAsync(properties);
        var authScheme = User.Claims.FirstOrDefault(x => string.Equals(x.Type, "auth_scheme"))?.Value;
        if (!string.IsNullOrWhiteSpace(authScheme))
        {
            await HttpContext.SignOutAsync(authScheme, properties);
        }
    }
}