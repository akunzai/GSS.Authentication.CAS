using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.Owin;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Newtonsoft.Json.Linq;
using Owin;
using Owin.OAuthGeneric;

[assembly: OwinStartup(typeof(OwinSingleSignOutSample.Startup))]

namespace OwinSingleSignOutSample
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

            app.UseErrorPage();
            var serviceCollection = new ServiceCollection();
            var redisConfiguration = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConfiguration))
            {
                serviceCollection.AddStackExchangeRedisCache(options => options.Configuration = redisConfiguration);
            }
            else
            {
                serviceCollection.AddDistributedMemoryCache();
            }
            serviceCollection.AddSingleton<IServiceTicketStore, DistributedCacheServiceTicketStore>();
            serviceCollection.AddSingleton<IAuthenticationSessionStore, AuthenticationSessionStoreWrapper>();
            var services = serviceCollection.BuildServiceProvider();
            app.UseCasSingleSignOut(services.GetRequiredService<IAuthenticationSessionStore>());
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/login"),
                LogoutPath = new PathString("/logout"),
                SessionStore = services.GetRequiredService<IAuthenticationSessionStore>(),
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
                // required for CasSingleSignOutMiddleware
                options.UseAuthenticationSessionStore = true;
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
                        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var user = JObject.Parse(json);
                        var identifier = user.Value<string>("id");
                        if (!string.IsNullOrEmpty(identifier))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier));
                        }
                        var attributes = user.Value<JObject>("attributes");
                        if (attributes == null)
                            return;
                        var displayName = attributes.Value<string>("display_name");
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                        }
                        var email = attributes.Value<string>("email");
                        if (!string.IsNullOrEmpty(email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                        }
                    }
                };
            });

            // Choose an authentication type
            app.Map("/login", branch =>
            {
                branch.Run(async context =>
                {
                    var scheme = context.Request.Query["authscheme"];
                    if (!string.IsNullOrEmpty(scheme))
                    {
                        // By default the client will be redirect back to the URL that issued the challenge (/login?authscheme=foo),
                        // send them to the home page instead (/).
                        context.Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" }, scheme);
                        return;
                    }

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(@"<!DOCTYPE html><html><head><meta charset=""utf-8""></head><body>");
                    await context.Response.WriteAsync("<p>Choose an authentication scheme:</p>");
                    foreach (var type in context.Authentication.GetAuthenticationTypes())
                    {
                        if (string.IsNullOrEmpty(type.Caption))
                            continue;
                        await context.Response.WriteAsync($"<a href=\"?authscheme={type.AuthenticationType}\">{type.Caption ?? type.AuthenticationType}</a><br>");
                    }
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            // Sign-out to remove the user cookie.
            app.Map("/logout", branch =>
            {
                branch.Run(context =>
                {
                    context.Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                    context.Response.Redirect("/");
                    return Task.CompletedTask;
                });
            });

            app.Run(async context =>
            {
                // CookieAuthenticationOptions.AutomaticAuthenticate = true (default) causes User to be set
                var user = context.Authentication.User;

                // This is what [Authorize] calls
                // var user = await context.Authentication.AuthenticateAsync(AuthenticationManager.AutomaticScheme);

                // Deny anonymous request beyond this point.
                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // This is what [Authorize] calls
                    // The cookie middleware will intercept this 401 and redirect to /login
                    context.Authentication.Challenge();

                    return;
                }

                // Display user information
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(@"<!DOCTYPE html><html><head><meta charset=""utf-8""></head><body>");
                await context.Response.WriteAsync($"<h1>Hello {user.Identity.Name ?? "anonymous"}</h1>");
                await context.Response.WriteAsync("<ul>");
                foreach (var claim in user.Claims)
                {
                    await context.Response.WriteAsync($"<li>{claim.Type}: {claim.Value}</li>");
                }
                await context.Response.WriteAsync("</ul>");
                await context.Response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
                await context.Response.WriteAsync("</body></html>");
            });
        }
    }
}
