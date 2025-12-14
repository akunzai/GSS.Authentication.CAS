using System.Net;
using System.Security.Claims;
using GSS.Authentication.CAS.Security;
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
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

public class CasAuthenticationMiddlewareTests
{
    private const string CasServerUrlBase = "https://cas.example.org/cas";

    [Fact]
    public async Task AnonymousRequest_WithRootPath_ShouldRedirectToLoginPath()
    {
        // Arrange
        using var host = CreateHost(options => options.CasServerUrlBase = CasServerUrlBase);
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();

        // Act
        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var loginUri = QueryHelpers.AddQueryString(
            new Uri(server.BaseAddress, CookieAuthenticationDefaults.LoginPath).AbsoluteUri,
            CookieAuthenticationDefaults.ReturnUrlParameter, "/");
        Assert.Equal(loginUri, response.Headers.Location?.AbsoluteUri);
    }

    [Fact]
    public async Task AnonymousRequest_WithCallbackPath_ShouldThrows()
    {
        // Arrange
        using var host = CreateHost(options => options.CasServerUrlBase = CasServerUrlBase);
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            // Act
            await client.GetAsync("/signin-cas", TestContext.Current.CancellationToken);
        });


        // Assert
        Assert.NotNull(exception);
        Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
        Assert.Equal("The state was missing or invalid", exception.InnerException!.Message);
    }

    [Fact]
    public async Task SignInChallenge_ShouldRedirectToCasServer()
    {
        // Arrange
        using var host = CreateHost(options => options.CasServerUrlBase = CasServerUrlBase);
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();

        // Act
        using var response = await client.GetAsync(CookieAuthenticationDefaults.LoginPath,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(CasServerUrlBase, response.Headers.Location?.AbsoluteUri ?? string.Empty);
    }

    [Fact]
    public async Task SignInChallenge_WithoutTicketInCallbackQuery_ShouldThrows()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator;
            options.CasServerUrlBase = CasServerUrlBase;
        });
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();
        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath, TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket,
                string.Empty);

        var exception = await Record.ExceptionAsync(async () =>
        {
            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);
        });

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
        Assert.Equal("Missing ticket parameter from query", exception.InnerException!.Message);
    }

    [Fact]
    public async Task SignInChallenge_WithoutValidPrincipal_ShouldThrows()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        var ticket = Guid.NewGuid().ToString();
        ticketValidator
            .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICasPrincipal?>(null));
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator;
            options.CasServerUrlBase = CasServerUrlBase;
        });
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();
        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath, TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);

        var exception = await Record.ExceptionAsync(async () =>
        {
            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);
        });

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
        Assert.Contains("Missing principal from", exception.InnerException!.Message);
    }

    [Fact]
    public async Task SignInChallenge_WithValidTicketAndPrincipal_ShouldResponseWithAuthCookies()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICasPrincipal?>(principal));
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator;
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
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();
        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath, TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);

        // Act
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);

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
        using var authorizedResponse = await client.SendAsync(authorizedRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
        var bodyText = await authorizedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(principal.GetPrincipalName(), bodyText);
        await ticketValidator
            .Received(1).ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignInChallenge_WithValidatingException_ShouldThrows()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        var ticket = Guid.NewGuid().ToString();
        ticketValidator
            .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new NotSupportedException("test"));
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator;
            options.CasServerUrlBase = CasServerUrlBase;
        });
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();
        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath, TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);

        var exception = await Record.ExceptionAsync(async () =>
        {
            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);
        });

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Equal("test", exception.InnerException!.Message);
    }

    [Fact]
    public async Task SignInChallenge_WithValidatingExceptionAndHandledResponse_ShouldRedirectToAccessDeniedPath()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        var ticket = Guid.NewGuid().ToString();
        ticketValidator
            .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new NotSupportedException("test"));
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator;
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
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();
        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath, TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);

        // Act
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
        Assert.Equal(CookieAuthenticationDefaults.AccessDeniedPath, signInResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task SignInChallenge_WithTicketCreatingException_ShouldThrows()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICasPrincipal?>(principal));
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator;
            options.CasServerUrlBase = CasServerUrlBase;
            options.Events = new CasEvents { OnCreatingTicket = _ => throw new NotSupportedException("test") };
        });
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();
        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath, TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);

        var exception = await Record.ExceptionAsync(async () =>
        {
            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);
        });

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Equal("test", exception.InnerException!.Message);
    }

    [Fact]
    public async Task SignInChallenge_WithTicketCreatingExceptionAndHandledResponse_ShouldRedirectToAccessDeniedPath()
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
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();
        var ticket = Guid.NewGuid().ToString();
        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath, TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);

        // Act
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
        Assert.Equal(CookieAuthenticationDefaults.AccessDeniedPath, signInResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task SingleSignOut_ShouldRedirectToCasServer()
    {
        // Arrange
        var ticketValidator = Substitute.For<IServiceTicketValidator>();
        var ticket = Guid.NewGuid().ToString();
        var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
        ticketValidator
            .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICasPrincipal?>(principal));
        using var host = CreateHost(options =>
        {
            options.ServiceTicketValidator = ticketValidator;
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
        }, options =>
        {
            options.Events.OnSigningOut = async context =>
            {
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
                var result = await authService.AuthenticateAsync(context.HttpContext, null);
                var authScheme = result.Properties?.Items[".AuthScheme"];
                if (string.Equals(authScheme, CasDefaults.AuthenticationType))
                {
                    options.CookieManager.DeleteCookie(context.HttpContext, options.Cookie.Name!,
                        context.CookieOptions);
                    await context.HttpContext.SignOutAsync(authScheme);
                }
            };
        });
        var server = host.GetTestServer();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using var client = server.CreateClient();
        using var challengeResponse =
            await client.GetAsync(CookieAuthenticationDefaults.LoginPath, TestContext.Current.CancellationToken);
        var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location?.Query);
        var validateUrl =
            QueryHelpers.AddQueryString(query[Constants.Parameters.Service]!, Constants.Parameters.Ticket, ticket);
        using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
        using var signInResponse = await client.SendAsync(signInRequest, TestContext.Current.CancellationToken);
        var signOutRequest = signInResponse.GetRequestWithCookies(CookieAuthenticationDefaults.LogoutPath);

        // Act
        using var signOutResponse = await client.SendAsync(signOutRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Found, signOutResponse.StatusCode);
        var cookie = signOutResponse.Headers.GetValues("Set-Cookie").Single();
        Assert.StartsWith(".AspNetCore.Cookies=;", cookie);
        var callbackUrl = QueryHelpers.AddQueryString("http://localhost/signout-callback-cas", "state", string.Empty);
        var expectedUrlPrefix =
            QueryHelpers.AddQueryString(CasServerUrlBase + Constants.Paths.Logout, "service", callbackUrl);
        Assert.StartsWith(expectedUrlPrefix, signOutResponse.Headers.Location?.AbsoluteUri ?? string.Empty);
        await ticketValidator
            .Received(1).ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static IHost CreateHost(Action<CasAuthenticationOptions> configureOptions,
        Action<CookieAuthenticationOptions>? configureCookie = null)
    {
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                var authBuilder = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCAS(configureOptions);
                if (configureCookie != null)
                {
                    authBuilder.AddCookie(configureCookie);
                }
                else
                {
                    authBuilder.AddCookie();
                }
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
                                    new AuthenticationProperties { RedirectUri = "/" });
                            });
                        });
                        app.Map(CookieAuthenticationDefaults.LogoutPath, signOutApp =>
                        {
                            signOutApp.Run(async context =>
                            {
                                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            });
                        });
                        app.Run(async context =>
                        {
                            // Deny anonymous request beyond this point.
                            if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
                            {
                                // This is what [Authorize] calls
                                // The cookie middleware will intercept this 401 and redirect to LoginPath
                                await context.ChallengeAsync();
                                return;
                            }

                            // Display authenticated principal name
                            if (context.User.Identity is ClaimsIdentity claimsIdentity)
                            {
                                await context.Response
                                    .WriteAsync(claimsIdentity.FindFirst(claimsIdentity.NameClaimType)?.Value ??
                                                string.Empty);
                            }
                        });
                    });
            }).Build();
    }
}
