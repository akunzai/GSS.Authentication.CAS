using System.Net;
using System.Security.Claims;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Testing;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Testing;
using Moq;
using Owin;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests;

public class CasAuthenticationMiddlewareTests
{
    private const string CasServerUrlBase = "https://cas.example.org/cas";

    [Fact]
    public async Task AnonymousRequest_ShouldRedirectToLoginPath()
    {
        // Arrange
        using var server = CreateServer(options => options.CasServerUrlBase = CasServerUrlBase);

        // Act
        using var response = await server.HttpClient.GetAsync("/").ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var loginUri = QueryHelpers.AddQueryString(
            new Uri(server.BaseAddress, CookieAuthenticationDefaults.LoginPath.Value).AbsoluteUri,
            CookieAuthenticationDefaults.ReturnUrlParameter, "/");
        Assert.Equal(loginUri, response.Headers.Location.AbsoluteUri);
    }

    [Fact]
    public async Task SignInChallenge_ShouldRedirectToCasServer()
    {
        // Arrange
        using var server = CreateServer(options => options.CasServerUrlBase = CasServerUrlBase);

        // Act
        using var response = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value)
            .ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(CasServerUrlBase, response.Headers.Location.AbsoluteUri);
    }

    [Fact]
    public async Task ValidatingAndCreatingTicketSuccess_ShouldResponseWithAuthCookies()
    {
        // Arrange
        var ticketValidator = new Mock<IServiceTicketValidator>();
        using var server = CreateServer(options =>
        {
            options.ServiceTicketValidator = ticketValidator.Object;
            options.CasServerUrlBase = CasServerUrlBase;
            options.Provider = new CasAuthenticationProvider
            {
                OnCreatingTicket = context =>
                {
                    var assertion = (context.Identity as CasIdentity)?.Assertion;
                    if (assertion == null)
                        return Task.CompletedTask;
                    context.Identity.AddClaim(new Claim(context.Identity.NameClaimType, assertion.PrincipalName));
                    return Task.CompletedTask;
                }
            };
        });
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);
        using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value)
            .ConfigureAwait(false);

        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        // Act
        using var signinRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signinResponse = await server.HttpClient.SendAsync(signinRequest).ConfigureAwait(false);

        // Assert
        var cookies = signinResponse.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies,
            x => x.StartsWith(CookieAuthenticationDefaults.CookiePrefix +
                              CookieAuthenticationDefaults.AuthenticationType));
        Assert.Contains(cookies,
            x => x.StartsWith(
                $"{CookieAuthenticationDefaults.CookiePrefix}Correlation.{CasDefaults.AuthenticationType}"));
        Assert.Equal("/", signinResponse.Headers.Location.OriginalString);

        using var authorizedRequest = signinResponse.GetRequestWithCookies("/");
        using var authorizedResponse = await server.HttpClient.SendAsync(authorizedRequest).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
        var bodyText = await authorizedResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.Equal(principal.GetPrincipalName(), bodyText);
        ticketValidator.Verify(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()),
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
            options.CasServerUrlBase = CasServerUrlBase;
        });
        var ticket = Guid.NewGuid().ToString();
        ticketValidator
            .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new NotSupportedException("test"));
        using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value)
            .ConfigureAwait(false);

        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        Exception? exception = null;
        try
        {
            // Act
            using var signinRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await server.HttpClient.SendAsync(signinRequest).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            exception = e;
        }

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("An error was encountered while handling the remote login.", exception?.Message);
        Assert.IsType<NotSupportedException>(exception?.InnerException);
    }

    [Fact]
    public async Task ValidatingTicketFailureWithHandledResponse_ShouldRedirectToAccessDeniedPath()
    {
        // Arrange
        var ticketValidator = new Mock<IServiceTicketValidator>();
        using var server = CreateServer(options =>
        {
            options.ServiceTicketValidator = ticketValidator.Object;
            options.CasServerUrlBase = CasServerUrlBase;
            options.Provider = new CasAuthenticationProvider
            {
                OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/Account/ExternalLoginFailure");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        var ticket = Guid.NewGuid().ToString();
        ticketValidator
            .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new NotSupportedException("test"));
        using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value)
            .ConfigureAwait(false);

        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        // Act
        using var signinRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signinResponse = await server.HttpClient.SendAsync(signinRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Found, signinResponse.StatusCode);
        Assert.Equal("/Account/ExternalLoginFailure", signinResponse.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task CreatingTicketFailureWithoutHandledResponse_ShouldThrows()
    {
        // Arrange
        var ticketValidator = new Mock<IServiceTicketValidator>();
        using var server = CreateServer(options =>
        {
            options.ServiceTicketValidator = ticketValidator.Object;
            options.CasServerUrlBase = CasServerUrlBase;
            options.Provider = new CasAuthenticationProvider
            {
                OnCreatingTicket = _ => throw new NotSupportedException("test")
            };
        });
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .Setup(x => x.ValidateAsync(ticket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value)
            .ConfigureAwait(false);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        Exception? exception = null;
        try
        {
            // Act
            using var signinRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await server.HttpClient.SendAsync(signinRequest).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            exception = e;
        }

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("An error was encountered while handling the remote login.", exception?.Message);
        Assert.IsType<NotSupportedException>(exception?.InnerException);
    }

    [Fact]
    public async Task CreatingTicketFailureWithHandledResponse_ShouldRedirectToAccessDeniedPath()
    {
        // Arrange
        using var server = CreateServer(options =>
        {
            options.CasServerUrlBase = CasServerUrlBase;
            options.Provider = new CasAuthenticationProvider
            {
                OnCreatingTicket = _ => throw new NotSupportedException("test"),
                OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/Account/ExternalLoginFailure");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        var ticket = Guid.NewGuid().ToString();

        using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value)
            .ConfigureAwait(false);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
        var validateUrl = QueryHelpers.AddQueryString(query["service"], "ticket", ticket);

        // Act
        using var signinRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signinResponse = await server.HttpClient.SendAsync(signinRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Found, signinResponse.StatusCode);
        Assert.Equal("/Account/ExternalLoginFailure", signinResponse.Headers.Location.OriginalString);
    }

    private static TestServer CreateServer(Action<CasAuthenticationOptions> configureOptions)
    {
        var options = new CasAuthenticationOptions();
        configureOptions.Invoke(options);
        return TestServer.Create(app =>
        {
            app.SetDataProtectionProvider(new FakeDataProtectionProvider(new AesDataProtector("test")));
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = CookieAuthenticationDefaults.LoginPath,
                LogoutPath = CookieAuthenticationDefaults.LogoutPath
            });
            app.UseCasAuthentication(options);
            app.Use(async (context, _) =>
            {
                var request = context.Request;

                if (request.Path == CookieAuthenticationDefaults.LoginPath)
                {
                    context.Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" },
                        CasDefaults.AuthenticationType);
                    return;
                }

                if (request.Path == CookieAuthenticationDefaults.LogoutPath)
                {
                    context.Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                    return;
                }

                var user = context.Authentication.User;

                // Deny anonymous request beyond this point.
                if (user?.Identities.Any(identity => identity.IsAuthenticated) != true)
                {
                    // This is what [Authorize] calls
                    // The cookie middleware will intercept this 401 and redirect to LoginPath
                    context.Authentication.Challenge();
                    return;
                }

                // Display authenticated principal name
                await context.Response.WriteAsync(user.GetPrincipalName()).ConfigureAwait(false);
            });
        });
    }
}