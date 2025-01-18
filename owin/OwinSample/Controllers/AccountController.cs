using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace OwinSample.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        // GET /Account/Login
        [HttpGet]
        public ActionResult Login(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                return View();
            }

            Request.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" }, scheme);
            return new HttpUnauthorizedResult();
        }

        // GET /Account/Logout
        [HttpGet]
        public void Logout(string redirectUrl)
        {
            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                redirectUrl = "/";
            }
            var owinContext = Request.GetOwinContext();
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            owinContext.Authentication.SignOut(properties, CookieAuthenticationDefaults.AuthenticationType);
            var authScheme = owinContext.Authentication.User.FindFirst("auth_scheme")?.Value;
            if (!string.IsNullOrWhiteSpace(authScheme))
            {
                owinContext.Authentication.SignOut(properties, authScheme);
            }
        }
    }
}