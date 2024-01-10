using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSample.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    public IActionResult OnGet(string scheme)
    {
        if (string.IsNullOrWhiteSpace(scheme))
        {
            return Page();
        }
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, scheme);
    }
}