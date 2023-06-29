using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlazorServerSample.Pages.Account;

public class Logout : PageModel
{
    public IActionResult OnGet()
    {
        return SignOut();
    }
}