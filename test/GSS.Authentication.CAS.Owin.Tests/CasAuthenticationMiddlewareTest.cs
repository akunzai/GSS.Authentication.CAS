using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public class CasAuthenticationMiddlewareTest : IClassFixture<CasFixture>,IDisposable
    {
        private readonly TestServer server;
        private readonly HttpClient client;
        private readonly CasFixture fixture;
        private IServiceTicketValidator ticketValidator;
        private ICasPrincipal principal;
        
        public CasAuthenticationMiddlewareTest(CasFixture fixture)
        {
            this.fixture = fixture;
            // Arrange
            var principalName = Guid.NewGuid().ToString();
            principal = new CasPrincipal(new Assertion(principalName), CasDefaults.AuthenticationType);
            ticketValidator = Mock.Of<IServiceTicketValidator>();
            server = TestServer.Create(app =>
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
                    ServiceTicketValidator = ticketValidator,
                    CasServerUrlBase = fixture.Options.CasServerUrlBase
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
            client = server.HttpClient;
        }

        public void Dispose()
        {
            server.Dispose();
        }

        [Fact]
        public async Task Challenge_RedirectToCasServerUrlAsync()
        {
            // Act
            var response = await client.GetAsync("/login");
            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.True(response.Headers.Location.AbsoluteUri.StartsWith(fixture.Options.CasServerUrlBase));
        }

        [Fact]
        public async Task CreatingTicket_SuccessAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            Mock.Get(ticketValidator)
                .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(principal);
            //// challenge to CAS login page
            var response = await client.GetAsync("/login");

            var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
            var serviceUrl = query["service"];
            var url = QueryHelpers.AddQueryString(serviceUrl, "ticket", ticket);

            //// validate service ticket & state
            var request = response.GetRequest(url);
            var validateResponse = await client.SendAsync(request);

            // Act : should got auth cookie
            request = validateResponse.GetRequest("/");
            var homeResponse = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
            var bodyText = await homeResponse.Content.ReadAsStringAsync();
            Assert.Equal(principal.GetPrincipalName(), bodyText);
            Mock.Get(ticketValidator).Verify(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
