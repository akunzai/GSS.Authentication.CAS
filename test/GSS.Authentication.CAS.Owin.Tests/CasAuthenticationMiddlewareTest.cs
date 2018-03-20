using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Tests;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Testing;
using Moq;
using Owin;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests
{
    public class CasAuthenticationMiddlewareTest : IClassFixture<CasFixture>, IDisposable
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly CasFixture _fixture;
        private readonly IServiceTicketValidator _ticketValidator;
        private readonly ICasPrincipal _principal;

        public CasAuthenticationMiddlewareTest(CasFixture fixture)
        {
            _fixture = fixture;
            // Arrange
            var principalName = Guid.NewGuid().ToString();
            _principal = new CasPrincipal(new Assertion(principalName), CasDefaults.AuthenticationType);
            _ticketValidator = Mock.Of<IServiceTicketValidator>();
            _server = TestServer.Create(app =>
            {
                app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    LoginPath = new PathString("/login"),
                    LogoutPath = new PathString("/logout")
                });
                app.UseCasAuthentication(new CasAuthenticationOptions
                {
                    CallbackPath = new PathString("/signin-cas"),
                    ServiceTicketValidator = _ticketValidator,
                    CasServerUrlBase = fixture.Options.CasServerUrlBase,
                    Provider = new CasAuthenticationProvider
                    {
                        OnCreatingTicket = context =>
                        {
                            var assertion = (context.Identity as CasIdentity)?.Assertion;
                            if (assertion == null) return Task.CompletedTask;
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                            return Task.CompletedTask;
                        }
                    }
                });
                app.Use(async (context, next) =>
                {
                    var request = context.Request;
                    if (request.Path.StartsWithSegments(new PathString("/login")))
                    {
                        context.Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" }, CasDefaults.AuthenticationType);
                        return;
                    }
                    if (request.Path.StartsWithSegments(new PathString("/logout")))
                    {
                        context.Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                    }
                    await next.Invoke();
                });
                app.Run(async context =>
                {
                    var user = context.Authentication.User;
                    // Deny anonymous request beyond this point.
                    if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                    {
                        context.Authentication.Challenge(CasDefaults.AuthenticationType);
                        return;
                    }
                    // Display authenticated principal name
                    await context.Response.WriteAsync(user.GetPrincipalName());
                });
            });
            _client = _server.HttpClient;
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        [Fact]
        public async Task Challenge_RedirectToCasServerUrlAsync()
        {
            // Act
            var response = await _client.GetAsync("/login");
            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith(_fixture.Options.CasServerUrlBase, response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task CreatingTicket_SuccessAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            Mock.Get(_ticketValidator)
                .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_principal);
            //// challenge to CAS login page
            var response = await _client.GetAsync("/login");

            var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
            var serviceUrl = query["service"];
            var url = QueryHelpers.AddQueryString(serviceUrl, "ticket", ticket);

            //// validate service ticket & state
            var request = response.GetRequest(url);
            var validateResponse = await _client.SendAsync(request);

            // Act : should got auth cookie
            request = validateResponse.GetRequest("/");
            var homeResponse = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
            var bodyText = await homeResponse.Content.ReadAsStringAsync();
            Assert.Equal(_principal.GetPrincipalName(), bodyText);
            Mock.Get(_ticketValidator).Verify(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
