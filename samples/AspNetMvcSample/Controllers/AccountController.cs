using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using AspNetMvcSample.Models;
using GSS.Authentication.CAS;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace AspNetMvcSample.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        // GET: /Account/Login?authtype=xxx
        [HttpGet]
        [AllowAnonymous]
        public void Login(string authtype)
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") };
            AuthenticationManager.Challenge(properties, string.IsNullOrWhiteSpace(authtype) ? CasDefaults.AuthenticationType : authtype);
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
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

        // GET: /Account/Me
        [Authorize]
        [HttpGet]
        public ActionResult Me()
        {
            return View();
        }

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;
    }
}