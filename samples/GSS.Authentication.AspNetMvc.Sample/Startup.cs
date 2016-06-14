using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using GSS.Authentication.CAS.Owin;
using GSS.Authentication.CAS.Security;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Newtonsoft.Json.Linq;
using Owin;
using Owin.OAuthGeneric;

[assembly: OwinStartup(typeof(GSS.Authentication.AspNetMvc.Sample.Startup))]
namespace GSS.Authentication.AspNetMvc.Sample
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // MVC
            GlobalFilters.Filters.Add(new HandleErrorAttribute());
            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            app.UseErrorPage();

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                LoginPath = CookieAuthenticationDefaults.LoginPath,
                LogoutPath = CookieAuthenticationDefaults.LogoutPath,
                ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter
            });

            app.UseCasAuthentication(new CasAuthenticationOptions
            {
                CasServerUrlBase = ConfigurationManager.AppSettings["Authentication:CAS:CasServerUrlBase"],
                Provider = new CasAuthenticationProvider
                {
                    OnCreatingTicket = (context) =>
                    {
                        // first_name, family_name, display_name, email, verified_email
                        var assertion = (context.Identity as CasIdentity)?.Assertion;
                        if (assertion == null) return Task.FromResult(0);
                        var email = assertion.Attributes["email"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                        }
                        var displayName = assertion.Attributes["display_name"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                        }
                        return Task.FromResult(0);
                    }
                }
            });

            app.UseOAuthAuthentication((options) => {
                options.ClientId = ConfigurationManager.AppSettings["Authentication:OAuth:ClientId"];
                options.ClientSecret = ConfigurationManager.AppSettings["Authentication:OAuth:ClientSecret"];
                options.CallbackPath = new PathString("/sign-oauth");
                options.AuthorizationEndpoint = ConfigurationManager.AppSettings["Authentication:OAuth:AuthorizationEndpoint"];
                options.TokenEndpoint = ConfigurationManager.AppSettings["Authentication:OAuth:TokenEndpoint"];
                options.SaveTokensAsClaims = true;
                options.UserInformationEndpoint = ConfigurationManager.AppSettings["Authentication:OAuth:UserInformationEndpoint"];
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async (context) =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var response = await context.Backchannel.SendAsync(request, context.Request.CallCancelled);
                        response.EnsureSuccessStatusCode();

                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());
                        var identifier = user.Value<string>("id");
                        if (!string.IsNullOrEmpty(identifier))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier));
                        }
                        var attributes = user.Value<JObject>("attributes");
                        if (attributes == null) return;
                        var email = attributes.Value<string>("email");
                        if (!string.IsNullOrEmpty(email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                        }
                        var displayName = attributes.Value<string>("display_name");
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                        }
                    }
                };
            });
        }
    }
}
