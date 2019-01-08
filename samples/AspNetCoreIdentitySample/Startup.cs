using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCoreIdentitySample.Data;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

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
                .AddDefaultUI(UIFramework.Bootstrap4)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddAuthentication()
                .AddCAS(options =>
                {
                    options.CallbackPath = "/signin-cas";
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
                    options.Events.OnCreatingTicket = context =>
                    {
                        // add claims from CasIdentity.Assertion ?
                        var assertion = context.Assertion;
                        if (assertion == null)
                            return Task.CompletedTask;
                        if (!(context.Principal.Identity is ClaimsIdentity identity))
                            return Task.CompletedTask;
                        // ClaimTypes.NameIdentifier was preferred by Microsoft.AspNetCore.Identity.SignInManager
                        context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                        if (assertion.Attributes.TryGetValue("email", out var email))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Email, email));
                        }
                        if (assertion.Attributes.TryGetValue("display_name", out var displayName))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.GivenName, displayName));
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
                options.Events.OnCreatingTicket = async context =>
                {
                    var request =
                        new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response =
                        await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();

                    var user = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var identifier = user.Value<string>("id");
                    if (!string.IsNullOrEmpty(identifier))
                    {
                        // ClaimTypes.NameIdentifier was preferred by Microsoft.AspNetCore.Identity.SignInManager
                        context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier));
                    }
                    var attributes = user.Value<JObject>("attributes");
                    if (attributes == null)
                        return;
                    var email = attributes.Value<string>("email");
                    if (!string.IsNullOrEmpty(email))
                    {
                        context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                    }
                    var name = attributes.Value<string>("display_name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        context.Identity.AddClaim(new Claim(ClaimTypes.GivenName, name));
                    }
                };
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
