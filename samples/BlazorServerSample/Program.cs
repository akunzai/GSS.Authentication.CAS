using System.Security.Claims;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
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
            var authScheme = result.Properties?.Items[".AuthScheme"];
            if (string.Equals(authScheme, CasDefaults.AuthenticationType) || string.Equals(authScheme, OpenIdConnectDefaults.AuthenticationScheme))
            {
                options.CookieManager.DeleteCookie(context.HttpContext, options.Cookie.Name!, context.CookieOptions);
                // redirecting to the identity provider to sign out
                await context.HttpContext.SignOutAsync(authScheme);
            }
        };
    })
    .AddCAS(options =>
    {
        options.CasServerUrlBase = builder.Configuration["CAS:ServerUrlBase"]!;
        options.SaveTokens = builder.Configuration.GetValue("CAS:SaveTokens", false);
        options.Events.OnCreatingTicket = context =>
        {
            if (context.Identity == null)
                return Task.CompletedTask;
            // Map claims from assertion
            var assertion = context.Assertion;
            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
            if (assertion.Attributes.TryGetValue("display_name", out var displayName) && !string.IsNullOrWhiteSpace(displayName))
            {
                context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName!));
            }
            if (assertion.Attributes.TryGetValue("cn", out var fullName) &&
                            !string.IsNullOrWhiteSpace(fullName))
            {
                context.Identity.AddClaim(new Claim(ClaimTypes.Name, fullName));
            }

            if (assertion.Attributes.TryGetValue("email", out var email) && !string.IsNullOrWhiteSpace(email))
            {
                context.Identity.AddClaim(new Claim(ClaimTypes.Email, email!));
            }

            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.ClientId = builder.Configuration["OIDC:ClientId"];
        options.ClientSecret = builder.Configuration["OIDC:ClientSecret"];
        options.Authority = builder.Configuration["OIDC:Authority"];
        options.MetadataAddress = builder.Configuration["OIDC:MetadataAddress"];
        options.ResponseType =
            builder.Configuration.GetValue("OIDC:ResponseType", OpenIdConnectResponseType.Code)!;
        options.ResponseMode =
            builder.Configuration.GetValue("OIDC:ResponseMode", OpenIdConnectResponseMode.Query)!;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.Scope.Clear();
        builder.Configuration.GetValue("OIDC:Scope", "openid profile email")!
            .Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(s => options.Scope.Add(s));
        options.SaveTokens = builder.Configuration.GetValue("OIDC:SaveTokens", false);
        options.TokenValidationParameters.NameClaimType = builder.Configuration.GetValue("OIDC:NameClaimType", "name");
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

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();