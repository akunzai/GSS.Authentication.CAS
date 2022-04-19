using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddAuthorization(options =>
{
    // Globally Require Authenticated Users
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events = new CookieAuthenticationEvents
        {
            OnSigningOut = context =>
            {
                // Single Sign-Out
                var casUrl = new Uri(builder.Configuration["Authentication:CAS:ServerUrlBase"]);
                var links = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
                var serviceUrl = links.GetUriByPage(context.HttpContext, "/Index");
                var redirectUri = UriHelper.BuildAbsolute(
                    casUrl.Scheme,
                    new HostString(casUrl.Host, casUrl.Port),
                    casUrl.LocalPath, "/logout",
                    QueryString.Create("service", serviceUrl!));

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
        options.CasServerUrlBase = builder.Configuration["Authentication:CAS:ServerUrlBase"];
        options.SaveTokens = builder.Configuration.GetValue("Authentication:CAS:SaveTokens",false);
        var protocolVersion = builder.Configuration.GetValue("Authentication:CAS:ProtocolVersion", 3);
        if (protocolVersion != 3)
        {
            options.ServiceTicketValidator = protocolVersion switch
            {
                1 => new Cas10ServiceTicketValidator(options),
                2 => new Cas20ServiceTicketValidator(options),
                _ => null
            };
        }

        options.Events = new CasEvents
        {
            OnCreatingTicket = context =>
            {
                if (context.Identity == null)
                    return Task.CompletedTask;
                // Map claims from assertion
                var assertion = context.Assertion;
                context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                if (assertion.Attributes.TryGetValue("display_name", out var displayName))
                {
                    context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                }

                if (assertion.Attributes.TryGetValue("email", out var email))
                {
                    context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                }

                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                var failure = context.Failure;
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
                if (!string.IsNullOrWhiteSpace(failure?.Message))
                {
                    logger.LogError(failure, "{Exception}", failure.Message);
                }

                context.Response.Redirect("/Account/ExternalLoginFailure");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    })
    .AddOAuth(OAuthDefaults.DisplayName, options =>
    {
        options.CallbackPath = "/signin-oauth";
        options.ClientId = builder.Configuration["Authentication:OAuth:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:OAuth:ClientSecret"];
        options.AuthorizationEndpoint = builder.Configuration["Authentication:OAuth:AuthorizationEndpoint"];
        options.TokenEndpoint = builder.Configuration["Authentication:OAuth:TokenEndpoint"];
        options.UserInformationEndpoint = builder.Configuration["Authentication:OAuth:UserInformationEndpoint"];
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonSubKey(ClaimTypes.Name, "attributes", "display_name");
        options.ClaimActions.MapJsonSubKey(ClaimTypes.Email, "attributes", "email");
        options.SaveTokens = builder.Configuration.GetValue("Authentication:OAuth:SaveTokens",false);
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
                    response.Content.Headers.ContentType?.MediaType?.StartsWith("application/json") != true)
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
                if (!string.IsNullOrWhiteSpace(failure?.Message))
                {
                    logger.LogError(failure, "{Exception}", failure.Message);
                }

                context.Response.Redirect("/Account/ExternalLoginFailure");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.ClientId = builder.Configuration["Authentication:OIDC:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:OIDC:ClientSecret"];
        options.Authority = builder.Configuration["Authentication:OIDC:Authority"];
        options.MetadataAddress = builder.Configuration["Authentication:OIDC:MetadataAddress"];
        options.ResponseType = builder.Configuration.GetValue("Authentication:OIDC:ResponseType", OpenIdConnectResponseType.Code);
        options.ResponseMode = builder.Configuration.GetValue("Authentication:OIDC:ResponseMode", OpenIdConnectResponseMode.Query);
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.Scope.Clear();
        builder.Configuration.GetValue("Authentication:OIDC:Scope", "openid profile email").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(s => options.Scope.Add(s));
        options.SaveTokens = builder.Configuration.GetValue("Authentication:OIDC:SaveTokens",false);
        options.Events = new OpenIdConnectEvents
        {
            OnRemoteFailure = context =>
            {
                var failure = context.Failure;
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OAuthEvents>>();
                if (!string.IsNullOrWhiteSpace(failure?.Message))
                {
                    logger.LogError(failure, "{Exception}", failure.Message);
                }

                context.Response.Redirect("/Account/ExternalLoginFailure");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });
builder.Logging
    .ClearProviders()
    .SetMinimumLevel(LogLevel.Trace)
    .AddNLogWeb();

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

app.MapRazorPages();

// configure nlog.config per environment
var envLogConfig =
    new FileInfo(Path.Combine(AppContext.BaseDirectory, $"nlog.{app.Environment.EnvironmentName}.config"));
var logger = NLogBuilder.ConfigureNLog(envLogConfig.Exists ? envLogConfig.Name : "nlog.config").GetCurrentClassLogger();
try
{
    app.Run();
}
catch (Exception exception)
{
    //NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}