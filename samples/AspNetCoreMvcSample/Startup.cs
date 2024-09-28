using System.Security.Claims;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AspNetCoreMvcSample;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
    {
        Configuration = configuration;
        HostingEnvironment = hostEnvironment;
    }

    public IConfiguration Configuration { get; }
    
    public IWebHostEnvironment HostingEnvironment { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var singleLogout = Configuration.GetValue("CAS:SingleLogout", false);
        if (singleLogout)
        {
            services.AddDistributedMemoryCache();
            var redisConfiguration = Configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConfiguration))
            {
                services.AddStackExchangeRedisCache(options => options.Configuration = redisConfiguration);
            }

            services.AddSingleton<ITicketStore, DistributedCacheTicketStore>();
            services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
                .Configure<ITicketStore>((o, t) => o.SessionStore = t);
        }
        services.AddControllersWithViews();
        services.AddAuthorization(options =>
        {
            // Globally Require Authenticated Users
            options.FallbackPolicy = options.DefaultPolicy;
        });
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie()
            .AddCAS(options =>
            {
                options.CasServerUrlBase = Configuration["CAS:ServerUrlBase"]!;
                // required for CasSingleLogoutMiddleware
                options.SaveTokens = singleLogout || Configuration.GetValue("CAS:SaveTokens", false);
                options.Events.OnCreatingTicket = context =>
                {
                    if (context.Identity == null)
                        return Task.CompletedTask;
                    // Map claims from assertion
                    var assertion = context.Assertion;
                    context.Identity.AddClaim(new Claim("auth_scheme", CasDefaults.AuthenticationType));
                    context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                    if (assertion.Attributes.TryGetValue("display_name", out var displayName) &&
                        !string.IsNullOrWhiteSpace(displayName))
                    {
                        context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName!));
                    }

                    if (assertion.Attributes.TryGetValue("cn", out var fullName) &&
                        !string.IsNullOrWhiteSpace(fullName))
                    {
                        context.Identity.AddClaim(new Claim(ClaimTypes.Name, fullName!));
                    }

                    if (assertion.Attributes.TryGetValue("email", out var email) &&
                        !string.IsNullOrWhiteSpace(email))
                    {
                        context.Identity.AddClaim(new Claim(ClaimTypes.Email, email!));
                    }

                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(options =>
            {
                options.ClientId = Configuration["OIDC:ClientId"];
                options.ClientSecret = Configuration["OIDC:ClientSecret"];
                options.Authority = Configuration["OIDC:Authority"];
                options.RequireHttpsMetadata = !HostingEnvironment.IsDevelopment();
                // required for single logout
                options.SaveTokens = Configuration.GetValue("OIDC:SaveTokens", false);
                options.ResponseType = OpenIdConnectResponseType.Code;
                var scope = Configuration["OIDC:Scope"];
                if (!string.IsNullOrWhiteSpace(scope))
                {
                    scope.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(s => options.Scope.Add(s));
                }
                options.TokenValidationParameters.NameClaimType =
                    Configuration.GetValue("OIDC:NameClaimType", "name");
                options.Events.OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity claimIdentity)
                    {
                        claimIdentity.AddClaim(new Claim("auth_scheme", OpenIdConnectDefaults.AuthenticationScheme));
                    }
                    return Task.CompletedTask;
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
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        var singleLogout = Configuration.GetValue("CAS:SingleLogout", false);
        if (singleLogout)
        {
            app.UseCasSingleLogout();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}