using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AspNetCoreIdentitySample.Data;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCoreIdentitySample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddDefaultIdentity<IdentityUser>()
                .AddDefaultUI()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddAuthentication()
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
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                            if (assertion.Attributes.TryGetValue("display_name", out var displayName))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                            }
                            if (assertion.Attributes.TryGetValue("email", out var email))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Email, email));
                            }
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
                    options.Events.OnCreatingTicket = async context =>
                    {
                        // Get the OAuth user
                        using var request =
                            new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        using var response =
                            await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted).ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode || response.Content?.Headers?.ContentType?.MediaType.StartsWith("application/json") != true)
                        {
                            throw new HttpRequestException($"An error occurred when retrieving OAuth user information ({response.StatusCode}). Please check if the authentication information is correct.");
                        }
                        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
                        context.RunClaimActions(json.RootElement);
                    };
                });
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

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
