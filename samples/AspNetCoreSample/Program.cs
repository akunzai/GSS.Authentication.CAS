using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.Metadata;

var builder = WebApplication.CreateBuilder(args);
var singleLogout = builder.Configuration.GetValue("CAS:SingleLogout", false);
if (singleLogout)
{
    builder.Services.AddDistributedMemoryCache();
    var redisConfiguration = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConfiguration))
    {
        builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConfiguration);
    }

    builder.Services.AddSingleton<ITicketStore, DistributedCacheTicketStore>();
    builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
        .Configure<ITicketStore>((o, t) => o.SessionStore = t);
}

builder.Services.AddRazorPages();
builder.Services.AddAuthorization(options =>
{
    // Globally Require Authenticated Users
    options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events.OnSigningOut = async context =>
        {
            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
            var result = await authService.AuthenticateAsync(context.HttpContext, null);
            string? authScheme = null;
            if (result.Properties != null || result.Properties!.Items.TryGetValue(".AuthScheme", out authScheme) &&
                string.IsNullOrWhiteSpace(authScheme))
            {
                if (string.Equals(authScheme, CasDefaults.AuthenticationType))
                {
                    options.CookieManager.DeleteCookie(context.HttpContext, options.Cookie.Name!,
                        context.CookieOptions);
                    // redirecting to the identity provider to sign out
                    await context.HttpContext.SignOutAsync(authScheme);
                    return;
                }

                if (string.Equals(authScheme, OpenIdConnectDefaults.AuthenticationScheme) &&
                    builder.Configuration.GetValue("OIDC:SaveTokens", false))
                {
                    options.CookieManager.DeleteCookie(context.HttpContext, options.Cookie.Name!,
                        context.CookieOptions);
                    // redirecting to the identity provider to sign out
                    await context.HttpContext.SignOutAsync(authScheme);
                    return;
                }
            }

            var saml2SessionIndex = context.HttpContext.User.FindFirst(Saml2ClaimTypes.SessionIndex);
            if (saml2SessionIndex != null)
            {
                // redirecting to the identity provider to sign out
                await context.HttpContext.SignOutAsync(Saml2Defaults.Scheme);
                return;
            }

            await context.Options.Events.RedirectToLogout(new RedirectContext<CookieAuthenticationOptions>(
                context.HttpContext,
                context.Scheme,
                context.Options,
                context.Properties,
                "/"
            ));
        };
    })
    .AddCAS(options =>
    {
        options.CasServerUrlBase = builder.Configuration["CAS:ServerUrlBase"]!;
        // required for CasSingleLogoutMiddleware
        options.SaveTokens = singleLogout || builder.Configuration.GetValue("CAS:SaveTokens", false);
        options.Events.OnCreatingTicket = context =>
        {
            if (context.Identity == null)
                return Task.CompletedTask;
            // Map claims from assertion
            var assertion = context.Assertion;
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

            if (assertion.Attributes.TryGetValue("email", out var email) && !string.IsNullOrWhiteSpace(email))
            {
                context.Identity.AddClaim(new Claim(ClaimTypes.Email, email!));
            }

            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            var failure = context.Failure;
            if (!string.IsNullOrWhiteSpace(failure?.Message))
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<CasAuthenticationHandler>>();
                logger.LogError(failure, "{Exception}", failure.Message);
            }

            context.Response.Redirect("/Account/ExternalLoginFailure");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.ClientId = builder.Configuration["OIDC:ClientId"];
        options.ClientSecret = builder.Configuration["OIDC:ClientSecret"];
        options.Authority = builder.Configuration["OIDC:Authority"];
        options.MetadataAddress = builder.Configuration["OIDC:MetadataAddress"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveTokens = builder.Configuration.GetValue("OIDC:SaveTokens", false);
        options.ResponseType =
            builder.Configuration.GetValue("OIDC:ResponseType", OpenIdConnectResponseType.Code)!;
        options.ResponseMode =
            builder.Configuration.GetValue("OIDC:ResponseMode", OpenIdConnectResponseMode.Query)!;
        var scope = builder.Configuration["OIDC:Scope"];
        if (!string.IsNullOrWhiteSpace(scope))
        {
            scope.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(s => options.Scope.Add(s));
        }

        options.TokenValidationParameters.NameClaimType =
            builder.Configuration.GetValue("OIDC:NameClaimType", "name");
        options.Events.OnRemoteFailure = context =>
        {
            var failure = context.Failure;
            if (!string.IsNullOrWhiteSpace(failure?.Message))
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OpenIdConnectHandler>>();
                logger.LogError(failure, "{Exception}", failure.Message);
            }

            context.Response.Redirect("/Account/ExternalLoginFailure");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    })
    .AddSaml2(options =>
    {
        options.SPOptions.EntityId = new EntityId(builder.Configuration["SAML2:SP:EntityId"]);
        options.SPOptions.AuthenticateRequestSigningBehavior = SigningBehavior.Never;
        var spSigningCertPath = builder.Configuration["SAML2:SP:SigningCertificate:Path"];
        if (!string.IsNullOrWhiteSpace(spSigningCertPath) && File.Exists(spSigningCertPath))
        {
            options.SPOptions.ServiceCertificates.Add(new X509Certificate2(
                spSigningCertPath,
                builder.Configuration["SAML2:SP:Certificate:Pass"],
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet));
            options.SPOptions.AuthenticateRequestSigningBehavior = SigningBehavior.IfIdpWantAuthnRequestsSigned;
        }

        options.SPOptions.TokenValidationParametersTemplate.NameClaimType = ClaimTypes.NameIdentifier;
        options.IdentityProviders.Add(
            new IdentityProvider(new EntityId(builder.Configuration["SAML2:IdP:EntityId"]), options.SPOptions)
            {
                MetadataLocation = builder.Configuration["SAML2:IdP:MetadataLocation"],
            });
        options.Notifications.MetadataCreated = (metadata, _) =>
        {
            var ssoDescriptor = metadata.RoleDescriptors.OfType<SpSsoDescriptor>().First();
            ssoDescriptor.WantAssertionsSigned = true;
        };
        // Avoid browsers downloading metadata as file
        options.Notifications.MetadataCommandResultCreated = result =>
        {
            result.ContentType = "application/xml";
            result.Headers.Remove("Content-Disposition");
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

if (singleLogout)
{
    app.UseCasSingleLogout();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();