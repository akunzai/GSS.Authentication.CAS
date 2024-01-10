using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;

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
        public void Logout()
        {
            Request.GetOwinContext().Authentication.SignOut();
        }

        // GET /Account/ExternalLoginFailure
        [HttpGet]
        public ActionResult ExternalLoginFailure()
        {
            Response.StatusCode = 500;
            return View();
        }
    }
}