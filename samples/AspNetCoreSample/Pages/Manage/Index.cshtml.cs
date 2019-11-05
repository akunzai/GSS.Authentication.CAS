using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSample.Pages.Manage
{
    [Authorize(Roles = "Manager")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {

        }
    }
}