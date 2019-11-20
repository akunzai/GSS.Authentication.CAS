using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSample.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        public string FormClass { get; set; } = "invisible";

        [Required]
        [BindProperty]
        public string Username { get; set; }

        [Required]
        [BindProperty]
        public string Password { get; set; }

        public IActionResult OnGet(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                return Page();
            }
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, scheme);
        }

        public IActionResult OnPost()
        {
            FormClass = string.Empty;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!(Username == "test" && Password == "test"))
            {
                ModelState.AddModelError(string.Empty, "The username or password is incorrect");
                return Page();
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(identity.NameClaimType, Username));
            return SignIn(new ClaimsPrincipal(identity),
                new AuthenticationProperties { RedirectUri = "/" },
                identity.AuthenticationType);
        }
    }
}