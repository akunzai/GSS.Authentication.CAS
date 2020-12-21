using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Testing;
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
    public class CasAuthenticationMiddlewareTest : IClassFixture<CasFixture>
    {
        private readonly ICasOptions _options;

        public CasAuthenticationMiddlewareTest(CasFixture fixture)
        {
            _options = fixture.Options;
        }

        [Fact]
        public async Task AnonymousRequest_ShouldRedirectToLoginPath()
        {
            // Arrange
            var ticketValidator = new Mock<IServiceTicketValidator>();
            using var server = CreateServer(options => options.CasServerUrlBase = _options.CasServerUrlBase);
            using var client = server.CreateClient();

            // Act
            using var response = await client.GetAsync("/").ConfigureAwait(false);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var loginUri = QueryHelpers.AddQueryString(new Uri(server.BaseAddress, CookieAuthenticationDefaults.LoginPath).AbsoluteUri, CookieAuthenticationDefaults.ReturnUrlParameter, "/");
            Assert.Equal(loginUri, response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SignInChallenge_ShouldRedirectToCasServer()
        {
            // Arrange
            using var server = CreateServer(options => options.CasServerUrlBase = _options.CasServerUrlBase);
            using var client = server.CreateClient();

            // Act
            using var response = await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith(_options.CasServerUrlBase, response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task ValidatingAndCreatingTicketSuccess_ShouldResponseWithAuthCookies()
        {
            // Arrange
            var ticketValidator = new Mock<IServiceTicketValidator>();
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator.Object;
                options.CasServerUrlBase = _options.CasServerUrlBase;
                options.Events = new CasEvents
                {
                    OnCreatingTicket = context =>
                    {
                        var assertion = context.Assertion;
                        if (assertion == null)
                            return Task.CompletedTask;
                        if (!(context.Principal.Identity is ClaimsIdentity identity))
                            return Task.CompletedTask;
                        identity.AddClaim(new Claim(identity.NameClaimType, assertion.PrincipalName));
                        return Task.CompletedTask;
                    }
                };
            });
            using var client = server.CreateClient();
            var ticket = Guid.NewGuid().ToString();
            var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
            ticketValidator
                .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(principal);

            using var challengeResponse = await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

            // Act
            using var signinRequest = challengeResponse.GetRequest(validateUrl);
            using var signinResponse = await client.SendAsync(signinRequest).ConfigureAwait(false);

            // Assert
            Assert.Equal(HttpStatusCode.Found, signinResponse.StatusCode);
            var cookies = signinResponse.Headers.GetValues("Set-Cookie");
            Assert.Equal(2, cookies.Count());
            Assert.Contains(cookies, x => x.StartsWith(CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme));
            Assert.Contains(cookies, x => x.StartsWith($"{CookieAuthenticationDefaults.CookiePrefix}Correlation"));
            Assert.Equal("/", signinResponse.Headers.Location.OriginalString);

            using var authorizedRequest = signinResponse.GetRequest("/");
            using var authorizedResponse = await client.SendAsync(authorizedRequest).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
            var bodyText = await authorizedResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.Equal(principal.GetPrincipalName(), bodyText);
            ticketValidator
                .Verify(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatingTicketFailureWithoutHandledResponse_ShouldThrows()
        {
            // Arrange
            var ticketValidator = new Mock<IServiceTicketValidator>();
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator.Object;
                options.CasServerUrlBase = _options.CasServerUrlBase;
            });
            using var client = server.CreateClient();
            var ticket = Guid.NewGuid().ToString();
            ticketValidator
                .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new NotSupportedException("test"));

            using var challengeResponse = await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

            Exception exception = null;
            try
            {
                // Act
                using var signinRequest = challengeResponse.GetRequest(validateUrl);
                await client.SendAsync(signinRequest).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                exception = e;
            }
            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
            Assert.IsType<NotSupportedException>(exception.InnerException);
        }

        [Fact]
        public async Task ValidatingTicketFailureWithHandledResponse_ShouldRedirectToAccessDeniedPath()
        {
            // Arrange
            var ticketValidator = new Mock<IServiceTicketValidator>();
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator.Object;
                options.CasServerUrlBase = _options.CasServerUrlBase;
                options.Events = new CasEvents
                {
                    OnRemoteFailure = context =>
                    {
                        context.Response.Redirect(CookieAuthenticationDefaults.AccessDeniedPath);
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
            using var client = server.CreateClient();
            var ticket = Guid.NewGuid().ToString();
            ticketValidator
                .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new NotSupportedException("test"));

            using var challengeResponse = await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

            // Act
            using var signinRequest = challengeResponse.GetRequest(validateUrl);
            using var signinResponse = await client.SendAsync(signinRequest).ConfigureAwait(false);

            // Assert
            Assert.Equal(HttpStatusCode.Found, signinResponse.StatusCode);
            Assert.Equal(CookieAuthenticationDefaults.AccessDeniedPath, signinResponse.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task CreatingTicketFailureWithoutHandledResponse_ShouldThrows()
        {
            // Arrange
            var ticketValidator = new Mock<IServiceTicketValidator>();
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator.Object;
                options.CasServerUrlBase = _options.CasServerUrlBase;
                options.Events = new CasEvents
                {
                    OnCreatingTicket = _ => throw new NotSupportedException("test")
                };
            });
            using var client = server.CreateClient();
            var ticket = Guid.NewGuid().ToString();
            var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
            ticketValidator
                .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(principal);

            using var challengeResponse = await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

            Exception exception = null;
            try
            {
                // Act
                using var signinRequest = challengeResponse.GetRequest(validateUrl);
                await client.SendAsync(signinRequest).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
            Assert.IsType<NotSupportedException>(exception.InnerException);
        }

        [Fact]
        public async Task CreatingTicketFailureWithHandledResponse_ShouldRedirectToAccessDeniedPath()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = _options.CasServerUrlBase;
                options.Events = new CasEvents
                {
                    OnCreatingTicket = _ => throw new NotSupportedException("test"),
                    OnRemoteFailure = context =>
                    {
                        context.Response.Redirect(CookieAuthenticationDefaults.AccessDeniedPath);
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
            using var client = server.CreateClient();
            var ticket = Guid.NewGuid().ToString();

            using var challengeResponse = await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

            // Act
            using var signinRequest = challengeResponse.GetRequest(validateUrl);
            using var signinResponse = await client.SendAsync(signinRequest).ConfigureAwait(false);

            // Assert
            Assert.Equal(HttpStatusCode.Found, signinResponse.StatusCode);
            Assert.Equal(CookieAuthenticationDefaults.AccessDeniedPath, signinResponse.Headers.Location.OriginalString);
        }

        private TestServer CreateServer(Action<CasAuthenticationOptions> configureOptions)
        {
            return new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .AddCAS(configureOptions);
            })
            .Configure(app =>
            {
                app.UseAuthentication();
                app.Use(async (context, _) =>
                {
                    var request = context.Request;

                    if (request.Path == CookieAuthenticationDefaults.LoginPath)
                    {
                        await context.ChallengeAsync(CasDefaults.AuthenticationType, new AuthenticationProperties { RedirectUri = "/" }).ConfigureAwait(false);
                        return;
                    }

                    if (request.Path == CookieAuthenticationDefaults.LogoutPath)
                    {
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
                        return;
                    }

                    var user = context.User;

                    // Deny anonymous request beyond this point.
                    if (user?.Identities.Any(identity => identity.IsAuthenticated) != true)
                    {
                        // This is what [Authorize] calls
                        // The cookie middleware will intercept this 401 and redirect to LoginPath
                        await context.ChallengeAsync().ConfigureAwait(false);
                        return;
                    }

                    // Display authenticated principal name
                    var claimsIdentity = user.Identity as ClaimsIdentity;
                    await context.Response.WriteAsync(claimsIdentity?.FindFirst(claimsIdentity.NameClaimType)?.Value).ConfigureAwait(false);
                });
            }));
        }
    }
}
