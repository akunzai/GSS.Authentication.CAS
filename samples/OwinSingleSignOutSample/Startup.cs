using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.Owin;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using NLog;
using NLog.Owin.Logging;
using Owin;
using Owin.OAuthGeneric;

[assembly: OwinStartup(typeof(OwinSingleSignOutSample.Startup))]

namespace OwinSingleSignOutSample
{
    public class Startup
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private static IConfiguration _configuration;
        private static IServiceProvider _resolver;

        public void Configuration(IAppBuilder app)
        {
            var env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            ConfigureServices(services);
            _resolver = services.BuildServiceProvider();

            // MVC
            GlobalFilters.Filters.Add(new AuthorizeAttribute());
            GlobalFilters.Filters.Add(new HandleErrorAttribute());
            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            app.UseNLog();
            app.UseErrorPage();

            app.UseCasSingleSignOut(_resolver.GetRequiredService<IAuthenticationSessionStore>());

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = CookieAuthenticationDefaults.LoginPath,
                LogoutPath = CookieAuthenticationDefaults.LogoutPath,
                SessionStore = _resolver.GetRequiredService<IAuthenticationSessionStore>(),
                Provider = new CookieAuthenticationProvider
                {
                    OnResponseSignedIn = context =>
                    {
                        var loginRedirectContext = new CookieApplyRedirectContext
                        (
                            context.OwinContext,
                            context.Options,
                            context.Properties.RedirectUri ?? "/"
                        );
                        context.Options.Provider.ApplyRedirect(loginRedirectContext);
                    },
                    OnResponseSignOut = context =>
                    {
                        // Single Sign-Out
                        var casUrl = new Uri(_configuration["Authentication:CAS:ServerUrlBase"]);
                        var serviceUrl = context.Request.Uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                        var redirectUri = new UriBuilder(casUrl);
                        redirectUri.Path += "/logout";
                        redirectUri.Query = $"service={Uri.EscapeDataString(serviceUrl)}";
                        var logoutRedirectContext = new CookieApplyRedirectContext
                        (
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
                options.CasServerUrlBase = _configuration["Authentication:CAS:ServerUrlBase"];
                options.ServiceUrlBase = _configuration.GetValue<Uri>("Authentication:CAS:ServiceUrlBase");
                // required for CasSingleSignOutMiddleware
                options.UseAuthenticationSessionStore = true;
                var protocolVersion = _configuration.GetValue("Authentication:CAS:ProtocolVersion", 3);
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
                options.ClientId = _configuration["Authentication:OAuth:ClientId"];
                options.ClientSecret = _configuration["Authentication:OAuth:ClientSecret"];
                options.AuthorizationEndpoint = _configuration["Authentication:OAuth:AuthorizationEndpoint"];
                options.TokenEndpoint = _configuration["Authentication:OAuth:TokenEndpoint"];
                options.UserInformationEndpoint = _configuration["Authentication:OAuth:UserInformationEndpoint"];
                options.SaveTokensAsClaims = true;
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        // Get the OAuth user
                        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        using var response = await context.Backchannel.SendAsync(request, context.Request.CallCancelled).ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode || response.Content?.Headers?.ContentType?.MediaType.StartsWith("application/json") != true)
                        {
                            var responseText = response.Content == null
                                ? string.Empty
                                : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            _logger.Error($"An error occurred when retrieving OAuth user information ({response.StatusCode}). {responseText}");
                            return;
                        }

                        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
                        var user = json.RootElement;
                        if (user.TryGetProperty("id", out var id))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.GetString()));
                        }
                        if (user.TryGetProperty("attributes", out var attributes))
                        {
                            if (attributes.TryGetProperty("display_name", out var displayName))
                            {
                                context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName.GetString()));
                            }
                            if (attributes.TryGetProperty("email", out var email))
                            {
                                context.Identity.AddClaim(new Claim(ClaimTypes.Email, email.GetString()));
                            }
                        }
                    },
                    OnRemoteFailure = context =>
                    {
                        var failure = context.Failure;
                        _logger.Error(failure, failure.Message);
                        context.Response.Redirect("/Account/ExternalLoginFailure");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var redisConfiguration = _configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConfiguration))
            {
                services.AddStackExchangeRedisCache(options => options.Configuration = redisConfiguration);
            }
            else
            {
                services.AddDistributedMemoryCache();
            }
            services
                .AddSingleton(_configuration)
                .AddSingleton<IServiceTicketStore, DistributedCacheServiceTicketStore>()
                .AddSingleton<IAuthenticationSessionStore, AuthenticationSessionStoreWrapper>()
                .BuildServiceProvider();
        }
    }
}
