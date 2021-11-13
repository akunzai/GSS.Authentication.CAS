using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;
using AspNetCoreIdentitySample.Data;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
    // .AddDefaultUI();
builder.Services.AddRazorPages();
// builder.Services.Configure<CookiePolicyOptions>(options =>
// {
//     // This lambda determines whether user consent for non-essential cookies is needed for a given request.
//     options.CheckConsentNeeded = _ => true;
//     options.MinimumSameSitePolicy = SameSiteMode.None;
// });
builder.Services.AddAuthentication()
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
            context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
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
                await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType?.StartsWith("application/json") != true)
            {
                throw new HttpRequestException($"An error occurred when retrieving OAuth user information ({response.StatusCode}). Please check if the authentication information is correct.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            context.RunClaimActions(json.RootElement);
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
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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
