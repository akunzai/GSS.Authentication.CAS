using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

/// <summary>
/// Tests for CasEvents event handling
/// </summary>
public class CasEventsTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaultEventHandlers()
    {
        // Arrange & Act
        var events = new CasEvents();

        // Assert
        Assert.NotNull(events.OnCreatingTicket);
        Assert.NotNull(events.OnRedirectToAuthorizationEndpoint);
        Assert.NotNull(events.OnRedirectToIdentityProviderForSignOut);
    }

    [Fact]
    public async Task OnCreatingTicket_DefaultHandler_ShouldReturnCompletedTask()
    {
        // Arrange
        var events = new CasEvents();
        var context = CreateCasCreatingTicketContext();

        // Act
        var task = events.OnCreatingTicket(context);

        // Assert
        Assert.True(task.IsCompleted);
        await task;
    }

    [Fact]
    public async Task OnRedirectToAuthorizationEndpoint_DefaultHandler_ShouldRedirect()
    {
        // Arrange
        var events = new CasEvents();
        var httpContext = new DefaultHttpContext();
        var redirectUri = "https://cas.example.com/login";
        var context = new RedirectContext<CasAuthenticationOptions>(
            httpContext,
            new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler)),
            new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" },
            new AuthenticationProperties(),
            redirectUri);

        // Act
        await events.OnRedirectToAuthorizationEndpoint(context);

        // Assert
        Assert.Equal(302, httpContext.Response.StatusCode);
        Assert.Equal(redirectUri, httpContext.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task OnRedirectToIdentityProviderForSignOut_DefaultHandler_ShouldReturnCompletedTask()
    {
        // Arrange
        var events = new CasEvents();
        var context = CreateCasRedirectContext();

        // Act
        var task = events.OnRedirectToIdentityProviderForSignOut(context);

        // Assert
        Assert.True(task.IsCompleted);
        await task;
    }

    [Fact]
    public async Task OnCreatingTicket_CustomHandler_ShouldBeInvoked()
    {
        // Arrange
        var handlerInvoked = false;
        var events = new CasEvents
        {
            OnCreatingTicket = context =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            }
        };
        var context = CreateCasCreatingTicketContext();

        // Act
        await events.OnCreatingTicket(context);

        // Assert
        Assert.True(handlerInvoked);
    }

    [Fact]
    public async Task OnRedirectToAuthorizationEndpoint_CustomHandler_ShouldBeInvoked()
    {
        // Arrange
        var handlerInvoked = false;
        var events = new CasEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            }
        };
        var httpContext = new DefaultHttpContext();
        var context = new RedirectContext<CasAuthenticationOptions>(
            httpContext,
            new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler)),
            new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" },
            new AuthenticationProperties(),
            "https://cas.example.com/login");

        // Act
        await events.OnRedirectToAuthorizationEndpoint(context);

        // Assert
        Assert.True(handlerInvoked);
    }

    [Fact]
    public async Task OnRedirectToIdentityProviderForSignOut_CustomHandler_ShouldBeInvoked()
    {
        // Arrange
        var handlerInvoked = false;
        var events = new CasEvents
        {
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            }
        };
        var context = CreateCasRedirectContext();

        // Act
        await events.OnRedirectToIdentityProviderForSignOut(context);

        // Assert
        Assert.True(handlerInvoked);
    }

    [Fact]
    public async Task CreatingTicket_VirtualMethod_ShouldInvokeOnCreatingTicket()
    {
        // Arrange
        var handlerInvoked = false;
        var events = new CasEvents
        {
            OnCreatingTicket = context =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            }
        };
        var context = CreateCasCreatingTicketContext();

        // Act
        await events.CreatingTicket(context);

        // Assert
        Assert.True(handlerInvoked);
    }

    [Fact]
    public async Task RedirectToAuthorizationEndpoint_VirtualMethod_ShouldInvokeOnRedirectToAuthorizationEndpoint()
    {
        // Arrange
        var handlerInvoked = false;
        var events = new CasEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            }
        };
        var httpContext = new DefaultHttpContext();
        var context = new RedirectContext<CasAuthenticationOptions>(
            httpContext,
            new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler)),
            new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" },
            new AuthenticationProperties(),
            "https://cas.example.com/login");

        // Act
        await events.RedirectToAuthorizationEndpoint(context);

        // Assert
        Assert.True(handlerInvoked);
    }

    [Fact]
    public async Task RedirectToIdentityProviderForSignOut_VirtualMethod_ShouldInvokeOnRedirectToIdentityProviderForSignOut()
    {
        // Arrange
        var handlerInvoked = false;
        var events = new CasEvents
        {
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            }
        };
        var context = CreateCasRedirectContext();

        // Act
        await events.RedirectToIdentityProviderForSignOut(context);

        // Assert
        Assert.True(handlerInvoked);
    }

    private static CasCreatingTicketContext CreateCasCreatingTicketContext()
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var assertion = new Assertion("testuser");
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, CasDefaults.AuthenticationType);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties();
        var backchannel = new HttpClient();

        return new CasCreatingTicketContext(principal, properties, httpContext, scheme, options, backchannel, assertion);
    }

    private static CasRedirectContext CreateCasRedirectContext()
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties();
        var redirectUri = "https://cas.example.com/logout";

        return new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);
    }
}
