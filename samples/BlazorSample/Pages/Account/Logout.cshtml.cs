using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlazorSample.Pages.Account;

public class Logout : PageModel
{
    public IActionResult OnGet()
    {
        return SignOut();
    }
}