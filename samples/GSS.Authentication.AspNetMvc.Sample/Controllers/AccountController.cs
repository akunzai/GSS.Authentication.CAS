using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using GSS.Authentication.AspNetMvc.Sample.Models;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace GSS.Authentication.AspNetMvc.Sample.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        // GET: /Account/Login?authtype=xxx
        [HttpGet]
        [AllowAnonymous]
        public void Login(string authtype)
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Index","Home") };
            AuthenticationManager.Challenge(properties, authtype);
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid && model.Username == "test" && model.Password == "test")
            {
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationType);
                identity.AddClaim(new Claim(ClaimTypes.Name, "test"));
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

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }
    }
}