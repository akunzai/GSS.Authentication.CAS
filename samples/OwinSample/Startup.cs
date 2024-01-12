using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using GSS.Authentication.CAS.Owin;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using NLog.Owin.Logging;
using Owin;
using OwinSample.DependencyInjection;

[assembly: OwinStartup(typeof(OwinSample.Startup))]

namespace OwinSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var singleLogout = configuration.GetValue("CAS:SingleLogout", false);
            var services = new ServiceCollection();
            if (singleLogout)
            {
                services.AddSingleLogout(configuration);
            }

            var resolver = services.BuildServiceProvider();

            // MVC
            GlobalFilters.Filters.Add(new AuthorizeAttribute());
            GlobalFilters.Filters.Add(new HandleErrorAttribute());
            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

            app.UseNLog();

            // https://github.com/aspnet/AspNetKatana/issues/332
            app.Use(async (context, next) =>
            {
                var proxyProtocol = context.Request.Headers["X-Forwarded-Proto"];
                if (string.Equals(proxyProtocol, "https", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Scheme = "https";
                }

                await next.Invoke();
            });

            if (singleLogout)
            {
                app.UseCasSingleLogout(resolver.GetRequiredService<IAuthenticationSessionStore>());
            }

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = CookieAuthenticationDefaults.LoginPath,
                LogoutPath = CookieAuthenticationDefaults.LogoutPath,
                // https://github.com/aspnet/AspNetKatana/wiki/System.Web-response-cookie-integration-issues
                CookieManager = new SystemWebCookieManager(),
                SessionStore = singleLogout ? resolver.GetRequiredService<IAuthenticationSessionStore>() : null,
                Provider = new CookieAuthenticationProvider
                {
                    OnResponseSignOut = context =>
                    {
                        var redirectContext = new CookieApplyRedirectContext
                        (
                            context.OwinContext,
                            context.Options,
                            "/"
                        );
                        if (configuration.GetValue("CAS:SingleSignOut", false))
                        {
                            context.Options.CookieManager.DeleteCookie(context.OwinContext, context.Options.CookieName,
                                context.CookieOptions);
                            // Single Sign-Out
                            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
                            var serviceUrl = urlHelper.Action("Index", "Home", null, context.Request.Scheme);
                            var redirectUri = new UriBuilder(configuration["CAS:ServerUrlBase"]!);
                            redirectUri.Path += "/logout";
                            redirectUri.Query = $"service={Uri.EscapeDataString(serviceUrl)}";
                            redirectContext.RedirectUri = redirectUri.Uri.AbsoluteUri;
                        }

                        context.Options.Provider.ApplyRedirect(redirectContext);
                    }
                }
            });

            app.UseCasAuthentication(options =>
            {
                options.CasServerUrlBase = configuration["CAS:ServerUrlBase"]!;
                options.ServiceUrlBase = configuration.GetValue<Uri>("CAS:ServiceUrlBase");
                // required for CasSingleLogoutMiddleware
                options.SaveTokens = singleLogout || configuration.GetValue("CAS:SaveTokens", false);
                // https://github.com/aspnet/AspNetKatana/wiki/System.Web-response-cookie-integration-issues
                options.CookieManager = new SystemWebCookieManager();
                var protocolVersion = configuration.GetValue("CAS:ProtocolVersion", 3);
                if (protocolVersion != 3)
                {
                    var httpClient = options.BackchannelFactory(options);
                    options.ServiceTicketValidator = protocolVersion switch
                    {
                        1 => new Cas10ServiceTicketValidator(options, httpClient),
                        2 => new Cas20ServiceTicketValidator(options, httpClient),
                        _ => options.ServiceTicketValidator
                    };
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
                        if (assertion.Attributes.TryGetValue("display_name", out var displayName) &&
                            !string.IsNullOrWhiteSpace(displayName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                        }
                        if (assertion.Attributes.TryGetValue("cn", out var fullName) &&
                            !string.IsNullOrWhiteSpace(fullName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, fullName));
                        }

                        if (assertion.Attributes.TryGetValue("email", out var email) &&
                            !string.IsNullOrWhiteSpace(email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                        }

                        return Task.CompletedTask;
                    }
                };
            });
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                CallbackPath = new PathString("/signin-oidc"),
                ClientId = configuration["OIDC:ClientId"],
                ClientSecret = configuration["OIDC:ClientSecret"],
                Authority = configuration["OIDC:Authority"],
                MetadataAddress = configuration["OIDC:MetadataAddress"],
                ResponseType =
                    configuration.GetValue("OIDC:ResponseType", OpenIdConnectResponseType.Code),
                ResponseMode =
                    configuration.GetValue("OIDC:ResponseMode", OpenIdConnectResponseMode.Query),
                // Avoid 404 error when redirecting to the callback path. see https://github.com/aspnet/AspNetKatana/issues/348
                RedeemCode = true,
                Scope = configuration.GetValue("OIDC:Scope", "openid profile email"),
                RequireHttpsMetadata = !env.Equals("Development", StringComparison.OrdinalIgnoreCase),
                SaveTokens = configuration.GetValue("OIDC:SaveTokens", false),
                TokenValidationParameters =
                {
                    NameClaimType = configuration.GetValue("OIDC:NameClaimType", "name")
                },
                // https://github.com/aspnet/AspNetKatana/wiki/System.Web-response-cookie-integration-issues
                CookieManager = new SystemWebCookieManager(),
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = notification =>
                    {
                        // generate the redirect_uri parameter automatically
                        if (string.IsNullOrWhiteSpace(notification.Options.RedirectUri))
                        {
                            notification.ProtocolMessage.RedirectUri =
                                notification.Request.Scheme + Uri.SchemeDelimiter +
                                notification.Request.Host + notification.Request.PathBase +
                                notification.Options.CallbackPath;
                        }

                        return Task.CompletedTask;
                    },
                    AuthorizationCodeReceived = notification =>
                    {
                        // generate the redirect_uri parameter automatically
                        if (string.IsNullOrWhiteSpace(notification.Options.RedirectUri))
                        {
                            notification.TokenEndpointRequest.RedirectUri =
                                notification.Request.Scheme + Uri.SchemeDelimiter +
                                notification.Request.Host + notification.Request.PathBase +
                                notification.Options.CallbackPath;
                        }

                        return Task.CompletedTask;
                    }
                }
            });
        }
    }
}