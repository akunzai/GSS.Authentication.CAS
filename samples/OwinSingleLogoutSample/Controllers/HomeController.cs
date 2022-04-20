using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security.Cookies;

namespace OwinSingleLogoutSample.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated) return View();
            var result = await Request.GetOwinContext().Authentication
                .AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationType);
            return View(result.Properties);
        }
    }
}