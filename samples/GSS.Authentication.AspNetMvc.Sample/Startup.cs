using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using GSS.Authentication.CAS.Owin;
using GSS.Authentication.CAS.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Newtonsoft.Json.Linq;
using Owin;
using Owin.OAuthGeneric;

[assembly: OwinStartup(typeof(GSS.Authentication.AspNetMvc.Sample.Startup))]
namespace GSS.Authentication.AspNetMvc.Sample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

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
                LoginPath = CookieAuthenticationDefaults.LoginPath,
                LogoutPath = CookieAuthenticationDefaults.LogoutPath,
                Provider = new CookieAuthenticationProvider
                {
                    OnResponseSignOut = (context) =>
                    {
                        // Single Sign-Out
                        var casUrl = new Uri(configuration["Authentication:CAS:CasServerUrlBase"]);
                        var serviceUrl = context.Request.Uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                        var redirectUri = new UriBuilder(casUrl);
                        redirectUri.Path += "/logout";
                        redirectUri.Query = $"service={Uri.EscapeDataString(serviceUrl)}";
                        var logoutRedirectContext = new CookieApplyRedirectContext(
                            context.OwinContext,
                            context.Options,
                            redirectUri.Uri.AbsoluteUri
                            );
                        context.Options.Provider.ApplyRedirect(logoutRedirectContext);
                    }
                }
            });

            app.UseCasAuthentication(new CasAuthenticationOptions
            {
                CasServerUrlBase = configuration["Authentication:CAS:CasServerUrlBase"],
                Provider = new CasAuthenticationProvider
                {
                    OnCreatingTicket = (context) =>
                    {
                        // add claims from CasIdentity.Assertion ?
                        var assertion = (context.Identity as CasIdentity)?.Assertion;
                        if (assertion == null) return Task.CompletedTask;
                        context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                        if (assertion.Attributes.TryGetValue("email", out var email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                        }
                        if (assertion.Attributes.TryGetValue("display_name", out var displayName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                        }
                        return Task.CompletedTask;
                    }
                }
            });

            app.UseOAuthAuthentication(options =>
            {
                options.ClientId = configuration["Authentication:OAuth:ClientId"];
                options.ClientSecret = configuration["Authentication:OAuth:ClientSecret"];
                options.CallbackPath = new PathString("/sign-oauth");
                options.AuthorizationEndpoint = configuration["Authentication:OAuth:AuthorizationEndpoint"];
                options.TokenEndpoint = configuration["Authentication:OAuth:TokenEndpoint"];
                options.SaveTokensAsClaims = true;
                options.UserInformationEndpoint = configuration["Authentication:OAuth:UserInformationEndpoint"];
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
