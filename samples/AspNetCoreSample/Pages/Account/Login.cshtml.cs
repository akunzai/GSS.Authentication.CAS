using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSample.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        public async Task OnGet(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                return;
            }

            await HttpContext.ChallengeAsync(scheme, new AuthenticationProperties { RedirectUri = "/" }).ConfigureAwait(false);
        }
    }
}