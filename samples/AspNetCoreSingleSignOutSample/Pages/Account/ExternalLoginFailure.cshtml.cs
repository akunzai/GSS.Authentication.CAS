using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSingleSignOutSample.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginFailureModel : PageModel
    {
        public IActionResult OnGet()
        {
            return StatusCode(401);
        }
    }
}