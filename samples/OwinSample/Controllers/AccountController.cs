using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;

namespace OwinSample.Controllers
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