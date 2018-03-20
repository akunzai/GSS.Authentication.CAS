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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests
{
    public class CasAuthenticationTest : IClassFixture<CasFixture>, IDisposable
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly CasFixture _fixture;
        private readonly IServiceTicketValidator _ticketValidator;
        private readonly ICasPrincipal _principal;

        public CasAuthenticationTest(CasFixture fixture)
        {
            _fixture = fixture;
            // Arrange
            var principalName = Guid.NewGuid().ToString();
            _principal = new CasPrincipal(new Assertion(principalName), CasDefaults.AuthenticationType);
            _ticketValidator = Mock.Of<IServiceTicketValidator>();
            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/login";
                        options.LogoutPath = "/logout";
                    })
                    .AddCAS(options =>
                    {
                        options.CallbackPath = "/signin-cas";
                        options.ServiceTicketValidator = _ticketValidator;
                        options.CasServerUrlBase = fixture.Options.CasServerUrlBase;
                        options.Events = new CasEvents
                        {
                            OnCreatingTicket = context =>
                            {
                                var assertion = context.Assertion;
                                if (assertion == null) return Task.CompletedTask;
                                if (!(context.Principal.Identity is ClaimsIdentity identity)) return Task.CompletedTask;
                                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                                return Task.CompletedTask;
                            }
                        };
                    });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        var request = context.Request;
                        if (request.Path.StartsWithSegments(new PathString("/login")))
                        {
                            await context.ChallengeAsync(CasDefaults.AuthenticationType, new AuthenticationProperties { RedirectUri = "/" });
                            return;
                        }
                        if (request.Path.StartsWithSegments(new PathString("/logout")))
                        {
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                        await next.Invoke();
                    });
                    app.Run(async context =>
                    {
                        var user = context.User;
                        // Deny anonymous request beyond this point.
                        if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                        {
                            await context.ChallengeAsync(CasDefaults.AuthenticationType);
                            return;
                        }
                        // Display authenticated user id
                        await context.Response.WriteAsync((user.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    });
                }));
            _client = _server.CreateClient();
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
            Mock.Get(_ticketValidator)
                .Verify(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
            Mock.Get(_ticketValidator)
                .Verify(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
