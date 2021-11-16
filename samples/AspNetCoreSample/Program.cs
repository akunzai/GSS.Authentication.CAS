using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
    var protocolVersion = builder.Configuration.GetValue("Authentication:CAS:ProtocolVersion", 3);
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
            if (context.Principal?.Identity is not ClaimsIdentity identity)
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
            if (!string.IsNullOrWhiteSpace(failure?.Message))
            {
                logger.LogError(failure, failure.Message);
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
                logger.LogError(failure, failure.Message);
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
    .AddNLogWeb(); // NLog: Setup NLog for Dependency injection

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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

app.MapRazorPages();

// configure nlog.config per environment
var envLogConfig = new FileInfo(Path.Combine(AppContext.BaseDirectory, $"nlog.{app.Environment.EnvironmentName}.config"));
var logger = NLogBuilder.ConfigureNLog(envLogConfig.Exists ? envLogConfig.Name : "nlog.config").GetCurrentClassLogger();
try
{
    logger.Debug("init main");
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