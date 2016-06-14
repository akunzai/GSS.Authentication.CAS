using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests
{
    public class CasAuthenticationMiddlewareTest : IDisposable
    {
        protected readonly TestServer server;
        protected readonly HttpClient client;
        protected CasAuthenticationOptions options;
        protected IServiceTicketValidator ticketValidator;
        protected ICasPrincipal principal;

        public CasAuthenticationMiddlewareTest()
        {
            // Arrange
            var principalName = Guid.NewGuid().ToString();
            principal = new CasPrincipal(new Assertion(principalName), "CAS");
            ticketValidator = Mock.Of<IServiceTicketValidator>();
            options = new CasAuthenticationOptions
            {
                ServiceTicketValidator = ticketValidator,
                CasServerUrlBase = "http://example.com/cas"
            };
            server = new TestServer(new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        AutomaticAuthenticate = true,
                        AutomaticChallenge = true,
                        LoginPath = new PathString("/login"),
                        LogoutPath = new PathString("/logout")
                    });
                    app.UseCasAuthentication(options);
                    app.Use(async (context, next) =>
                    {
                        var request = context.Request;
                        if (request.Path.StartsWithSegments(new PathString("/login")))
                        {
                            await context.Authentication.ChallengeAsync(options.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
                        }
                        else if (request.Path.StartsWithSegments(new PathString("/logout")))
                        {
                            await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                        await next.Invoke();
                    });
                    app.Run(async context =>
                    {
                        var user = context.User;
                        // Deny anonymous request beyond this point.
                        if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                        {
                            await context.Authentication.ChallengeAsync(options.AuthenticationScheme);
                            return;
                        }
                        // Display authenticated user id
                        await context.Response.WriteAsync((user.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    });
                }).ConfigureServices(services =>
                {
                    services.AddAuthentication(opts => opts.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
                }));
            client = server.CreateClient();
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
            Assert.True(response.Headers.Location.AbsoluteUri.StartsWith(options.CasServerUrlBase));
            Mock.Get(ticketValidator)
                .Verify(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
            Mock.Get(ticketValidator)
                .Verify(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}
