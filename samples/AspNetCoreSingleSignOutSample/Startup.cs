using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AspNetCoreSingleSignOutSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private IServiceProvider Services { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var redisConfiguration = Configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConfiguration))
            {
                services.AddStackExchangeRedisCache(options => options.Configuration = redisConfiguration);
            }
            else
            {
                services.AddDistributedMemoryCache();
            }
            services.AddSingleton<IServiceTicketStore, DistributedCacheServiceTicketStore>();
            services.AddSingleton<ITicketStore, TicketStoreWrapper>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.SessionStore = Services.GetRequiredService<ITicketStore>();
                options.Events.OnSigningOut = context =>
                {
                    // Single Sign-Out
                    var casUrl = new Uri(Configuration["Authentication:CAS:ServerUrlBase"]);
                    var serviceUrl = new Uri(context.Request.GetEncodedUrl())
                        .GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                    var redirectUri = UriHelper.BuildAbsolute(
                        casUrl.Scheme,
                        new HostString(casUrl.Host, casUrl.Port),
                        casUrl.LocalPath, "/logout",
                        QueryString.Create("service", serviceUrl));

                    var logoutRedirectContext = new RedirectContext<CookieAuthenticationOptions>(
                        context.HttpContext,
                        context.Scheme,
                        context.Options,
                        context.Properties,
                        redirectUri
                    );
                    context.Response.StatusCode = 204; //Prevent RedirectToReturnUrl
                    context.Options.Events.RedirectToLogout(logoutRedirectContext);
                    return Task.CompletedTask;
                };
            })
            .AddCAS(options =>
            {
                options.CallbackPath = "/signin-cas";
                options.CasServerUrlBase = Configuration["Authentication:CAS:ServerUrlBase"];
                // required for CasSingleSignOutMiddleware
                options.SaveTokens = true;
                var protocolVersion = Configuration.GetValue("Authentication:CAS:ProtocolVersion", 3);
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
                options.Events.OnCreatingTicket = context =>
                {
                    var assertion = context.Assertion;
                    if (assertion == null)
                        return Task.CompletedTask;
                    if (!(context.Principal.Identity is ClaimsIdentity identity))
                        return Task.CompletedTask;
                    // Map claims from assertion
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                    if (assertion.Attributes.TryGetValue("display_name", out var displayName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                    }
                    if (assertion.Attributes.TryGetValue("email", out var email))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Email, email));
                    }
                    return Task.CompletedTask;
                };
            })
            .AddOAuth("OAuth", options =>
            {
                options.CallbackPath = "/signin-oauth";
                options.ClientId = Configuration["Authentication:OAuth:ClientId"];
                options.ClientSecret = Configuration["Authentication:OAuth:ClientSecret"];
                options.AuthorizationEndpoint = Configuration["Authentication:OAuth:AuthorizationEndpoint"];
                options.TokenEndpoint = Configuration["Authentication:OAuth:TokenEndpoint"];
                options.UserInformationEndpoint = Configuration["Authentication:OAuth:UserInformationEndpoint"];
                options.SaveTokens = true;
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonSubKey(ClaimTypes.Name, "attributes", "display_name");
                options.ClaimActions.MapJsonSubKey(ClaimTypes.Email, "attributes", "email");
                options.Events.OnCreatingTicket = async context =>
                {
                    // Get the OAuth user
                    var request =
                        new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    var response =
                        await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"An error occurred when retrieving OAuth user information ({response.StatusCode}). Please check if the authentication information is correct.");
                    }
                    using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    context.RunClaimActions(user.RootElement);
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Services = app.ApplicationServices;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseCasSingleSignOut();
            app.UseAuthentication();

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
                        await context.ChallengeAsync(scheme, new AuthenticationProperties { RedirectUri = "/" });
                        return;
                    }

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(@"<!DOCTYPE html><html><head><meta charset=""utf-8""></head><body>");
                    await context.Response.WriteAsync("<p>Choose an authentication scheme:</p>");
                    foreach (var type in context.RequestServices.GetRequiredService<IOptions<AuthenticationOptions>>().Value.Schemes)
                    {
                        if (string.IsNullOrEmpty(type.DisplayName))
                            continue;
                        await context.Response.WriteAsync($"<a href=\"?authscheme={type.Name}\">{type.DisplayName ?? type.Name}</a><br>");
                    }
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            // Sign-out to remove the user cookie.
            app.Map("/logout", branch =>
            {
                branch.Run(async context =>
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/");
                });
            });

            app.Run(async context =>
            {
                // CookieAuthenticationOptions.AutomaticAuthenticate = true (default) causes User to be set
                var user = context.User;

                // This is what [Authorize] calls
                // var user = await context.Authentication.AuthenticateAsync(AuthenticationManager.AutomaticScheme);

                // Deny anonymous request beyond this point.
                if (user?.Identities.Any(identity => identity.IsAuthenticated) != true)
                {
                    // This is what [Authorize] calls
                    // The cookie middleware will intercept this 401 and redirect to /login
                    await context.ChallengeAsync();
                    return;
                }

                // Display user information
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(@"<!DOCTYPE html><html><head><meta charset=""utf-8""></head><body>");
                await context.Response.WriteAsync($"<h1>Hello {user.Identity.Name ?? "anonymous"}</h1>");
                await context.Response.WriteAsync("<ul>");
                foreach (var claim in user.Claims)
                {
                    await context.Response.WriteAsync($"<li>{claim.Type}: {claim.Value}<br>");
                }
                await context.Response.WriteAsync("</ul>");
                await context.Response.WriteAsync("Tokens:<ol>");
                await context.Response.WriteAsync($"<li>Access Token: {await context.GetTokenAsync("access_token")}</li>");
                await context.Response.WriteAsync($"<li>Refresh Token: {await context.GetTokenAsync("refresh_token")}</li>");
                await context.Response.WriteAsync($"<li>Token Type: {await context.GetTokenAsync("token_type")}</li>");
                await context.Response.WriteAsync($"<li>Expires At: {await context.GetTokenAsync("expires_at")}</li>");
                await context.Response.WriteAsync("</ol>");
                await context.Response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
                await context.Response.WriteAsync("</body></html>");
            });
        }
    }
}