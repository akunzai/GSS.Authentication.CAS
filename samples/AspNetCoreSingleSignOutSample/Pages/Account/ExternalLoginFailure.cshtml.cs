using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSample.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginFailureModel : PageModel
    {
        private static readonly string _defaultFaulureMessage = "Unsuccessful login with service.";

        public string FailureMessage { get;set;} = _defaultFaulureMessage;

        public void OnGet(string failureMessage)
        {
            FailureMessage = failureMessage;
        }
    }
}