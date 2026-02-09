using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspireSample.Pages.Account;

public class LogoutModel : PageModel
{
    public async Task OnGet(string? redirectUrl)
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