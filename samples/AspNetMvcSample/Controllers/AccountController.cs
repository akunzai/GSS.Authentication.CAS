using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using AspNetMvcSample.Models;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace AspNetMvcSample.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/ExternalLoginFailure
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

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

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid && model.Username == "test" && model.Password == "test")
            {
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationType);
                identity.AddClaim(new Claim(identity.NameClaimType, model.Username));
                AuthenticationManager.SignIn(identity);
            }
            return RedirectToAction("Index", "Home");
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