using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSample.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginFailureModel : PageModel
    {
        public IActionResult OnGet()
        {
            Response.StatusCode = 500;
            return Page();
        }
    }
}