using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using OwinSingleSignOutSample.Models;

namespace OwinSingleSignOutSample.Controllers
{
    public class AccountController : Controller
    {
        // GET /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                return View();
            }

            Request.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" }, scheme);
            return new EmptyResult();
        }

        // POST /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            ViewData["formClass"] = string.Empty;
            if (!ModelState.IsValid)
            {
                return View();
            }

            if (!(model.Username == "test" && model.Password == "test"))
            {
                ModelState.AddModelError(string.Empty, "The username or password is incorrect");
                return View();
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationType);
            identity.AddClaim(new Claim(identity.NameClaimType, model.Username));
            Request.GetOwinContext().Authentication.SignIn(identity);
            return Redirect("/");
        }

        // GET /Account/Logout
        [HttpGet]
        public void Logout()
        {
            Request.GetOwinContext().Authentication.SignOut();
        }

        // GET /Account/ExternalLoginFailure
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            Response.StatusCode = 401;
            return View();
        }
    }
}