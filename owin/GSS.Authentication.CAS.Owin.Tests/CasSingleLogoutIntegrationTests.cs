using System.Net;
using System.Net.Http;
using System.Security.Claims;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Testing;
using NSubstitute;
using Owin;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests
{
    /// <summary>
    /// End-to-end proof of exactly when a back-channel CAS logout takes effect on an already-signed-in cookie
    /// session, backed by <see cref="DistributedCacheIAuthenticationSessionStore"/> as the cookie
    /// authentication's session store.
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
            var store = new DistributedCacheIAuthenticationSessionStore(
                new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
                Options.Create(new DistributedCacheIAuthenticationSessionStoreOptions()));

            using var server = TestServer.Create(app =>
            {
                app.SetDataProtectionProvider(new FakeDataProtectionProvider(new AesDataProtector("test")));
                app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    LoginPath = CookieAuthenticationDefaults.LoginPath,
                    LogoutPath = CookieAuthenticationDefaults.LogoutPath,
                    SessionStore = store
                });
                app.UseCasAuthentication(new CasAuthenticationOptions
                {
                    ServiceTicketValidator = ticketValidator,
                    CasServerUrlBase = CasServerUrlBase,
                    SaveTokens = true
                });
                app.Map("/cas-logout", logoutApp => logoutApp.UseCasSingleLogout(store));
                app.Use(async (context, _) =>
                {
                    if (context.Request.Path.Value == "/login")
                    {
                        context.Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" },
                            CasDefaults.AuthenticationType);
                        return;
                    }

                    var user = context.Authentication.User;
                    context.Response.StatusCode =
                        user?.Identities.Any(identity => identity.IsAuthenticated) == true ? 200 : 401;
                    await Task.CompletedTask;
                });
            });

            using var challengeResponse =
                await server.HttpClient.GetAsync("/login", TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            using var signInResponse =
                await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);

            using var authorizedRequest = signInResponse.GetRequestWithCookies("/");
            using var authorizedResponse =
                await server.HttpClient.SendAsync(authorizedRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);

            // Act
            using var logoutContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logoutRequest"] =
                    $@"<samlp:LogoutRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" ID=""{Guid.NewGuid()}"" Version=""2.0"" IssueInstant=""{DateTime.UtcNow:o}"">
    <saml:NameID xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"">@NOT_USED@</saml:NameID>
    <samlp:SessionIndex>{ticket}</samlp:SessionIndex></samlp:LogoutRequest>"
            });
            using var logoutResponse =
                await server.HttpClient.PostAsync("/cas-logout", logoutContent, TestContext.Current.CancellationToken);

            // Assert: the very next request with the same (still unexpired) cookie is no longer authenticated —
            // removing the session-store entry takes effect immediately, with no polling interval or caching delay.
            using var postLogoutRequest = signInResponse.GetRequestWithCookies("/");
            using var postLogoutResponse =
                await server.HttpClient.SendAsync(postLogoutRequest, TestContext.Current.CancellationToken);
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
            var store = new DistributedCacheIAuthenticationSessionStore(
                new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
                Options.Create(new DistributedCacheIAuthenticationSessionStoreOptions()));

            using var server = TestServer.Create(app =>
            {
                app.SetDataProtectionProvider(new FakeDataProtectionProvider(new AesDataProtector("test")));
                app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    LoginPath = CookieAuthenticationDefaults.LoginPath,
                    LogoutPath = CookieAuthenticationDefaults.LogoutPath
                });
                app.UseCasAuthentication(new CasAuthenticationOptions
                {
                    ServiceTicketValidator = ticketValidator,
                    CasServerUrlBase = CasServerUrlBase,
                    SaveTokens = true
                });
                app.Map("/cas-logout", logoutApp => logoutApp.UseCasSingleLogout(store));
                app.Use(async (context, _) =>
                {
                    if (context.Request.Path.Value == "/login")
                    {
                        context.Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" },
                            CasDefaults.AuthenticationType);
                        return;
                    }

                    var user = context.Authentication.User;
                    context.Response.StatusCode =
                        user?.Identities.Any(identity => identity.IsAuthenticated) == true ? 200 : 401;
                    await Task.CompletedTask;
                });
            });

            using var challengeResponse =
                await server.HttpClient.GetAsync("/login", TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            using var signInResponse =
                await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);

            // Act
            using var logoutContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logoutRequest"] =
                    $@"<samlp:LogoutRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" ID=""{Guid.NewGuid()}"" Version=""2.0"" IssueInstant=""{DateTime.UtcNow:o}"">
    <saml:NameID xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"">@NOT_USED@</saml:NameID>
    <samlp:SessionIndex>{ticket}</samlp:SessionIndex></samlp:LogoutRequest>"
            });
            using var logoutResponse =
                await server.HttpClient.PostAsync("/cas-logout", logoutContent, TestContext.Current.CancellationToken);

            // Assert: with no SessionStore wired, the cookie is self-contained, so the back-channel logout
            // removing the (otherwise-unused) store entry has no bearing on it — the session stays valid.
            using var postLogoutRequest = signInResponse.GetRequestWithCookies("/");
            using var postLogoutResponse =
                await server.HttpClient.SendAsync(postLogoutRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, postLogoutResponse.StatusCode);
        }
    }
}
