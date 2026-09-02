using System.Net;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

/// <summary>
/// End-to-end proof of exactly when a back-channel CAS logout takes effect on an already-signed-in cookie
/// session, backed by <see cref="DistributedCacheTicketStore"/> as the cookie authentication's session store.
/// </summary>
public class CasSingleLogoutIntegrationTests
{
    private const string CasServerUrlBase = "https://cas.example.org/cas";

    [Fact]
    public async Task SignedInSession_AfterBackChannelLogout_ShouldBeUnauthenticatedOnNextRequest()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICasPrincipal?>(principal));
        var store = new DistributedCacheTicketStore(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            Options.Create(new DistributedCacheTicketStoreOptions()));

        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCAS(options =>
                    {
                        options.ServiceTicketValidator = ticketValidator;
                        options.CasServerUrlBase = CasServerUrlBase;
                        options.SaveTokens = true;
                    })
                    .AddCookie(options => options.SessionStore = store);
            })
            .ConfigureWebHost(webHostBuilder => webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Map("/login", loginApp => loginApp.Run(context =>
                        context.ChallengeAsync(CasDefaults.AuthenticationType,
                            new AuthenticationProperties { RedirectUri = "/" })));
                    app.Map("/cas-logout", logoutApp => logoutApp.UseCasSingleLogout(store));
                    app.Run(context =>
                    {
                        context.Response.StatusCode =
                            context.User.Identity?.IsAuthenticated == true
                                ? (int)HttpStatusCode.OK
                                : (int)HttpStatusCode.Unauthorized;
                        return Task.CompletedTask;
                    });
                }))
            .Build();
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();

        using var challengeResponse = await client.GetAsync("/login", TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);

        using var authorizedRequest = signInResponse.GetRequestWithCookies("/");
        using var authorizedResponse =
            await client.SendAsync(authorizedRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);

        // Act
        using var logoutContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["logoutRequest"] =
                $@"<samlp:LogoutRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" ID=""{Guid.NewGuid()}"" Version=""2.0"" IssueInstant=""{DateTime.UtcNow:o}"">
    <saml:NameID xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"">@NOT_USED@</saml:NameID>
    <samlp:SessionIndex>{ticket}</samlp:SessionIndex></samlp:LogoutRequest>"
        });
        using var logoutResponse = await client.PostAsync("/cas-logout", logoutContent, TestContext.Current.CancellationToken);

        // Assert: the very next request with the same (still unexpired) cookie is no longer authenticated —
        // removing the session-store entry takes effect immediately, with no polling interval or caching delay.
        using var postLogoutRequest = signInResponse.GetRequestWithCookies("/");
        using var postLogoutResponse =
            await client.SendAsync(postLogoutRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, postLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task SignedInSession_WithoutSessionStoreWired_ShouldStayAuthenticatedAfterBackChannelLogout()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICasPrincipal?>(principal));
        // A store is still registered for UseCasSingleLogout to remove from, but it's never wired into
        // CookieAuthenticationOptions.SessionStore, so the cookie carries the full ticket, not a store key.
        var store = new DistributedCacheTicketStore(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            Options.Create(new DistributedCacheTicketStoreOptions()));

        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCAS(options =>
                    {
                        options.ServiceTicketValidator = ticketValidator;
                        options.CasServerUrlBase = CasServerUrlBase;
                        options.SaveTokens = true;
                    })
                    .AddCookie();
            })
            .ConfigureWebHost(webHostBuilder => webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Map("/login", loginApp => loginApp.Run(context =>
                        context.ChallengeAsync(CasDefaults.AuthenticationType,
                            new AuthenticationProperties { RedirectUri = "/" })));
                    app.Map("/cas-logout", logoutApp => logoutApp.UseCasSingleLogout(store));
                    app.Run(context =>
                    {
                        context.Response.StatusCode =
                            context.User.Identity?.IsAuthenticated == true
                                ? (int)HttpStatusCode.OK
                                : (int)HttpStatusCode.Unauthorized;
                        return Task.CompletedTask;
                    });
                }))
            .Build();
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();

        using var challengeResponse = await client.GetAsync("/login", TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);

        // Act
        using var logoutContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["logoutRequest"] =
                $@"<samlp:LogoutRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" ID=""{Guid.NewGuid()}"" Version=""2.0"" IssueInstant=""{DateTime.UtcNow:o}"">
    <saml:NameID xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"">@NOT_USED@</saml:NameID>
    <samlp:SessionIndex>{ticket}</samlp:SessionIndex></samlp:LogoutRequest>"
        });
        using var logoutResponse = await client.PostAsync("/cas-logout", logoutContent, TestContext.Current.CancellationToken);

        // Assert: with no SessionStore wired, the cookie is self-contained, so the back-channel logout removing
        // the (otherwise-unused) store entry has no bearing on it — the session stays valid.
        using var postLogoutRequest = signInResponse.GetRequestWithCookies("/");
        using var postLogoutResponse =
            await client.SendAsync(postLogoutRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, postLogoutResponse.StatusCode);
    }
}
