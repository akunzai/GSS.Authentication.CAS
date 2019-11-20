using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetCoreSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                // Global Authorize Filter
                var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });
            services.AddRazorPages();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Events = new CookieAuthenticationEvents
                {
                    OnSigningOut = context =>
                    {
                        // Single Sign-Out
                        var casUrl = new Uri(Configuration["Authentication:CAS:ServerUrlBase"]);
                        var links = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
                        var serviceUrl = links.GetUriByPage(context.HttpContext, "/Index");
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
                    }
                };
            })
            .AddCAS(options =>
            {
                options.CasServerUrlBase = Configuration["Authentication:CAS:ServerUrlBase"];
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
                options.Events = new CasEvents
                {
                    OnCreatingTicket = context =>
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
                    },
                    OnRemoteFailure = context =>
                    {
                        var failure = context.Failure;
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
                        logger.LogError(failure, failure.Message);
                        context.Response.Redirect("/Account/ExternalLoginFailure");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            })
            .AddOAuth(OAuthDefaults.DisplayName, options =>
            {
                options.CallbackPath = "/signin-oauth";
                options.ClientId = Configuration["Authentication:OAuth:ClientId"];
                options.ClientSecret = Configuration["Authentication:OAuth:ClientSecret"];
                options.AuthorizationEndpoint = Configuration["Authentication:OAuth:AuthorizationEndpoint"];
                options.TokenEndpoint = Configuration["Authentication:OAuth:TokenEndpoint"];
                options.UserInformationEndpoint = Configuration["Authentication:OAuth:UserInformationEndpoint"];
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonSubKey(ClaimTypes.Name, "attributes", "display_name");
                options.ClaimActions.MapJsonSubKey(ClaimTypes.Email, "attributes", "email");
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        // Get the OAuth user
                        using var request =
                            new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        using var response =
                            await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted)
                                .ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode ||
                            response.Content?.Headers?.ContentType?.MediaType.StartsWith("application/json") != true)
                        {
                            throw new HttpRequestException(
                                $"An error occurred when retrieving OAuth user information ({response.StatusCode}). Please check if the authentication information is correct.");
                        }

                        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
                        context.RunClaimActions(json.RootElement);
                    },
                    OnRemoteFailure = context =>
                    {
                        var failure = context.Failure;
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OAuthEvents>>();
                        logger.LogError(failure, failure.Message);
                        context.Response.Redirect("/Account/ExternalLoginFailure");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}