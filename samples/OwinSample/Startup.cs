using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using GSS.Authentication.CAS.Owin;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Owin.OAuthGeneric;

[assembly: OwinStartup(typeof(OwinSample.Startup))]

namespace OwinSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .Build();

            // MVC
            GlobalFilters.Filters.Add(new AuthorizeAttribute());
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
                    OnResponseSignOut = context =>
                    {
                        // Single Sign-Out
                        var casUrl = new Uri(configuration["Authentication:CAS:ServerUrlBase"]);
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

            app.UseCasAuthentication(options =>
            {
                options.CasServerUrlBase = configuration["Authentication:CAS:ServerUrlBase"];
                options.ServiceUrlBase = configuration.GetValue<Uri>("Authentication:CAS:ServiceUrlBase");
                var protocolVersion = configuration.GetValue("Authentication:CAS:ProtocolVersion", 3);
                if (protocolVersion != 3)
                {
                    switch (protocolVersion)
                    {
                        case 1:
                            options.ServiceTicketValidator = new Cas10ServiceTicketValidator(options);
                            break;
                        case 2:
                            options.ServiceTicketValidator = new Cas20ServiceTicketValidator(options);
                            break;
                    }
                }
                options.Provider = new CasAuthenticationProvider
                {
                    OnCreatingTicket = context =>
                    {
                        var assertion = (context.Identity as CasIdentity)?.Assertion;
                        if (assertion == null)
                            return Task.CompletedTask;
                        // Map claims from assertion
                        context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                        if (assertion.Attributes.TryGetValue("display_name", out var displayName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                        }
                        if (assertion.Attributes.TryGetValue("email", out var email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            app.UseOAuthAuthentication(options =>
            {
                options.ClientId = configuration["Authentication:OAuth:ClientId"];
                options.ClientSecret = configuration["Authentication:OAuth:ClientSecret"];
                options.CallbackPath = new PathString("/sign-oauth");
                options.AuthorizationEndpoint = configuration["Authentication:OAuth:AuthorizationEndpoint"];
                options.TokenEndpoint = configuration["Authentication:OAuth:TokenEndpoint"];
                options.UserInformationEndpoint = configuration["Authentication:OAuth:UserInformationEndpoint"];
                options.SaveTokensAsClaims = true;
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        // Get the OAuth user
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        var response = await context.Backchannel.SendAsync(request, context.Request.CallCancelled).ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException($"An error occurred when retrieving OAuth user information ({response.StatusCode}). Please check if the authentication information is correct.");
                        }
                        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
                        var user = json.RootElement;
                        if (user.TryGetProperty("id", out var identifier))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier.GetString()));
                        }
                        if (!user.TryGetProperty("attributes", out var attributes))
                            return;

                        if (attributes.TryGetProperty("display_name", out var displayName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName.GetString()));
                        }
                        if (attributes.TryGetProperty("email", out var email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Email, email.GetString()));
                        }
                    }
                };
            });
        }
    }
}
