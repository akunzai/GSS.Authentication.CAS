using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.AspNetCore;
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
    .AddCookie()
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
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        // required for single logout
        options.SaveTokens = builder.Configuration.GetValue("OIDC:SaveTokens", false);
        options.ResponseType = OpenIdConnectResponseType.Code;
        var scope = builder.Configuration["OIDC:Scope"];
        if (!string.IsNullOrWhiteSpace(scope))
        {
            scope.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(s => options.Scope.Add(s));
        }
        options.TokenValidationParameters.NameClaimType =
            builder.Configuration.GetValue("OIDC:NameClaimType", "name");
        options.Events.OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity claimIdentity)
            {
                claimIdentity.AddClaim(new Claim("auth_scheme", OpenIdConnectDefaults.AuthenticationScheme));
            }
            return Task.CompletedTask;
        };
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
        var signingCertPath = builder.Configuration["SAML2:SP:SigningCertificate:Path"];
        if (!string.IsNullOrWhiteSpace(signingCertPath) && File.Exists(signingCertPath))
        {
            // required for single logout
            options.SPOptions.ServiceCertificates.Add(new ServiceCertificate
            {
                Use = CertificateUse.Signing,
                Certificate = X509CertificateLoader.LoadPkcs12FromFile(
                signingCertPath,
                builder.Configuration["SAML2:SP:SigningCertificate:Pass"],
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet)
            });
            options.SPOptions.AuthenticateRequestSigningBehavior = SigningBehavior.IfIdpWantAuthnRequestsSigned;
        }

        options.SPOptions.TokenValidationParametersTemplate.NameClaimType = ClaimTypes.NameIdentifier;
        options.IdentityProviders.Add(
            new IdentityProvider(new EntityId(builder.Configuration["SAML2:IdP:EntityId"]), options.SPOptions)
            {
                MetadataLocation = builder.Configuration["SAML2:IdP:MetadataLocation"],
            });
        options.Notifications.AcsCommandResultCreated = (result,_) =>
        {
            if (result.Principal?.Identity is ClaimsIdentity claimIdentity)
            {
                claimIdentity.AddClaim(new Claim("auth_scheme", Saml2Defaults.Scheme));
            }
        };
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
