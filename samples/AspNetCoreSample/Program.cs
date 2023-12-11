using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
var singleLogout = builder.Configuration.GetValue("CAS:SingleLogout", false);
if (singleLogout)
{
    builder.Services.AddDistributedMemoryCache();
    var redisConfiguration = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConfiguration))
    {
        builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConfiguration);
    }

    builder.Services.AddSingleton<IServiceTicketStore, DistributedCacheServiceTicketStore>();
    builder.Services.AddSingleton<ITicketStore, TicketStoreWrapper>();
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
                logger.Error(failure, "{Exception}", failure.Message);
            }

            context.Response.Redirect("/Account/ExternalLoginFailure");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    })
    .AddOAuth(OAuthDefaults.DisplayName, options =>
    {
        options.CallbackPath = "/signin-oauth";
        options.ClientId = builder.Configuration["OAuth:ClientId"]!;
        options.ClientSecret = builder.Configuration["OAuth:ClientSecret"]!;
        options.AuthorizationEndpoint = builder.Configuration["OAuth:AuthorizationEndpoint"]!;
        options.TokenEndpoint = builder.Configuration["OAuth:TokenEndpoint"]!;
        options.UserInformationEndpoint = builder.Configuration["OAuth:UserInformationEndpoint"]!;
        builder.Configuration.GetValue("OAuth:Scope", "openid profile email")!
            .Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(s => options.Scope.Add(s));
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        options.ClaimActions.MapJsonSubKey(ClaimTypes.Name, "attributes", "display_name");
        options.ClaimActions.MapJsonSubKey(ClaimTypes.Email, "attributes", "email");
        options.SaveTokens = builder.Configuration.GetValue("OAuth:SaveTokens", false);
        options.Events.OnCreatingTicket = async context =>
        {
            // Get the OAuth user
            using var request =
                new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Accept.ParseAdd("application/json");
            if (builder.Configuration.GetValue("OAuth:SendAccessTokenInQuery", false))
            {
                request.RequestUri =
                    new Uri(QueryHelpers.AddQueryString(request.RequestUri!.OriginalString, "access_token",
                        context.AccessToken!));
            }
            else
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", context.AccessToken);
            }

            using var response =
                await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted)
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode ||
                response.Content.Headers.ContentType?.MediaType?.StartsWith("application/json") != true)
            {
                throw new HttpRequestException(
                    $"An error occurred when retrieving OAuth user information ({response.StatusCode}). Please check if the authentication information is correct.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            context.RunClaimActions(json.RootElement);
        };
        options.Events.OnRemoteFailure = context =>
        {
            var failure = context.Failure;
            if (!string.IsNullOrWhiteSpace(failure?.Message))
            {
                logger.Error(failure, "{Exception}", failure.Message);
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
        options.ResponseType =
            builder.Configuration.GetValue("OIDC:ResponseType", OpenIdConnectResponseType.Code)!;
        options.ResponseMode =
            builder.Configuration.GetValue("OIDC:ResponseMode", OpenIdConnectResponseMode.Query)!;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.Scope.Clear();
        builder.Configuration.GetValue("OIDC:Scope", "openid profile email")!
            .Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(s => options.Scope.Add(s));
        options.SaveTokens = builder.Configuration.GetValue("OIDC:SaveTokens", false);
        options.Events.OnRemoteFailure = context =>
        {
            var failure = context.Failure;
            if (!string.IsNullOrWhiteSpace(failure?.Message))
            {
                logger.Error(failure, "{Exception}", failure.Message);
            }

            context.Response.Redirect("/Account/ExternalLoginFailure");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

// Setup NLog for Dependency injection
builder.Logging.ClearProviders().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Host.UseNLog();

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

try
{
    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}