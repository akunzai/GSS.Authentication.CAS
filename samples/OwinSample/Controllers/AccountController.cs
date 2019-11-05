using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace OwinSample.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                return View();
            }
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") };
            AuthenticationManager.Challenge(properties, scheme);
            return new EmptyResult();
        }

        // GET: /Account/Logout
        [HttpGet]
        public ActionResult Logout()
        {
            AuthenticationManager.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return RedirectToAction("Index", "Home");
        }

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;
    }
}