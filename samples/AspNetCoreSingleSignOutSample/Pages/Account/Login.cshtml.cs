using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSingleSignOutSample.Pages.Account
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

        public async Task OnGet(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                return;
            }

            await HttpContext.ChallengeAsync(scheme, new AuthenticationProperties { RedirectUri = "/" }).ConfigureAwait(false);
        }

        public async Task<IActionResult> OnPost()
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
            await HttpContext.SignInAsync(new ClaimsPrincipal(identity)).ConfigureAwait(false);
            return Redirect("/");
        }
    }
}