using System.Net;
using System.Security.Claims;
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
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

public class CasAuthenticationMiddlewareTests
{
    private const string CasServerUrlBase = "https://cas.example.org/cas";

    [Fact]
    public async Task AnonymousRequest_ShouldRedirectToLoginPath()
    {
        // Arrange
        using var host = CreateHost(options => options.CasServerUrlBase = CasServerUrlBase);
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();

        // Act
        using var response = await client.GetAsync("/").ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var loginUri = QueryHelpers.AddQueryString(
            new Uri(server.BaseAddress, CookieAuthenticationDefaults.LoginPath).AbsoluteUri,
            CookieAuthenticationDefaults.ReturnUrlParameter, "/");
        Assert.Equal(loginUri, response.Headers.Location?.AbsoluteUri);
    }

    [Fact]
    public async Task SignInChallenge_ShouldRedirectToCasServer()
    {
        // Arrange
        using var host = CreateHost(options => options.CasServerUrlBase = CasServerUrlBase);
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();

        // Act
        using var response = await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(CasServerUrlBase, response.Headers.Location?.AbsoluteUri ?? string.Empty);
    }

    [Fact]
    public async Task ValidatingAndCreatingTicketSuccess_ShouldResponseWithAuthCookies()
    {
        // Arrange
        var ticketValidator = new Mock<IServiceTicketValidator>();
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator.Object;
            options.CasServerUrlBase = CasServerUrlBase;
            options.Events = new CasEvents
            {
                OnCreatingTicket = context =>
                {
                    var assertion = context.Assertion;
                    if (context.Principal?.Identity is not ClaimsIdentity identity)
                        return Task.CompletedTask;
                    identity.AddClaim(new Claim(identity.NameClaimType, assertion.PrincipalName));
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        // Act
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
        var cookies = signInResponse.Headers.GetValues("Set-Cookie").ToList();
        Assert.Equal(2, cookies.Count);
        Assert.Contains(cookies,
            x => x.StartsWith(CookieAuthenticationDefaults.CookiePrefix +
                              CookieAuthenticationDefaults.AuthenticationScheme));
        Assert.Contains(cookies, x => x.StartsWith($"{CookieAuthenticationDefaults.CookiePrefix}Correlation"));
        Assert.Equal("/", signInResponse.Headers.Location?.OriginalString);

        using var authorizedRequest = signInResponse.GetRequestWithCookies("/");
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
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator.Object;
            options.CasServerUrlBase = CasServerUrlBase;
        });
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();
        var ticket = Guid.NewGuid().ToString();
        ticketValidator
            .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new NotSupportedException("test"));

        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        Exception? exception = null;
        try
        {
            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await client.SendAsync(signInRequest).ConfigureAwait(false);
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
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator.Object;
            options.CasServerUrlBase = CasServerUrlBase;
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
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();
        var ticket = Guid.NewGuid().ToString();
        ticketValidator
            .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new NotSupportedException("test"));

        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        // Act
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
        Assert.Equal(CookieAuthenticationDefaults.AccessDeniedPath, signInResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreatingTicketFailureWithoutHandledResponse_ShouldThrows()
    {
        // Arrange
        var ticketValidator = new Mock<IServiceTicketValidator>();
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator.Object;
            options.CasServerUrlBase = CasServerUrlBase;
            options.Events = new CasEvents { OnCreatingTicket = _ => throw new NotSupportedException("test") };
        });
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        Exception? exception = null;
        try
        {
            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await client.SendAsync(signInRequest).ConfigureAwait(false);
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
        using var host = CreateHost(options =>
        {
            options.CasServerUrlBase = CasServerUrlBase;
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
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();
        var ticket = Guid.NewGuid().ToString();

        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath).ConfigureAwait(false);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        // Act
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
        Assert.Equal(CookieAuthenticationDefaults.AccessDeniedPath, signInResponse.Headers.Location?.OriginalString);
    }

    private static IHost CreateHost(Action<CasAuthenticationOptions> configureOptions)
    {
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie()
                    .AddCAS(configureOptions);
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Map(CookieAuthenticationDefaults.LoginPath, signInApp =>
                        {
                            signInApp.Run(async context =>
                            {
                                await context.ChallengeAsync(CasDefaults.AuthenticationType,
                                    new AuthenticationProperties { RedirectUri = "/" }).ConfigureAwait(false);
                            });
                        });
                        app.Map(CookieAuthenticationDefaults.LogoutPath, signOutApp =>
                        {
                            signOutApp.Run(async context =>
                            {
                                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                                    .ConfigureAwait(false);
                            });
                        });
                        app.Run(async context =>
                        {
                            // Deny anonymous request beyond this point.
                            if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
                            {
                                // This is what [Authorize] calls
                                // The cookie middleware will intercept this 401 and redirect to LoginPath
                                await context.ChallengeAsync().ConfigureAwait(false);
                                return;
                            }

                            // Display authenticated principal name
                            if (context.User.Identity is ClaimsIdentity claimsIdentity)
                            {
                                await context.Response
                                    .WriteAsync(claimsIdentity.FindFirst(claimsIdentity.NameClaimType)?.Value ??
                                                string.Empty).ConfigureAwait(false);
                            }
                        });
                    });
            }).Build();
    }
}