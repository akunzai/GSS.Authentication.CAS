using System;
using System.Net.Http;
using System.Security.Claims;
using GSS.Authentication.CAS.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

/// <summary>
/// Tests for CasCreatingTicketContext
/// </summary>
public class CasCreatingTicketContextTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var assertion = new Assertion("testuser");
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, CasDefaults.AuthenticationType);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties();
        var backchannel = new HttpClient();

        // Act
        var context = new CasCreatingTicketContext(principal, properties, httpContext, scheme, options, backchannel, assertion);

        // Assert
        Assert.NotNull(context);
        Assert.Same(principal, context.Principal);
        Assert.Same(properties, context.Properties);
        Assert.Same(backchannel, context.Backchannel);
        Assert.Same(assertion, context.Assertion);
        Assert.Same(httpContext, context.HttpContext);
        Assert.Same(scheme, context.Scheme);
        Assert.Same(options, context.Options);
    }

    [Fact]
    public void Constructor_WithNullBackchannel_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var assertion = new Assertion("testuser");
        var principal = new ClaimsPrincipal();
        var properties = new AuthenticationProperties();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new CasCreatingTicketContext(principal, properties, httpContext, scheme, options, null!, assertion));
        Assert.Equal("backchannel", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullAssertion_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var principal = new ClaimsPrincipal();
        var properties = new AuthenticationProperties();
        var backchannel = new HttpClient();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new CasCreatingTicketContext(principal, properties, httpContext, scheme, options, backchannel, null!));
        Assert.Equal("assertion", exception.ParamName);
    }

    [Fact]
    public void Identity_WithClaimsPrincipal_ShouldReturnClaimsIdentity()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var assertion = new Assertion("testuser");
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, CasDefaults.AuthenticationType);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties();
        var backchannel = new HttpClient();

        // Act
        var context = new CasCreatingTicketContext(principal, properties, httpContext, scheme, options, backchannel, assertion);

        // Assert
        Assert.NotNull(context.Identity);
        Assert.Same(identity, context.Identity);
        Assert.Equal("testuser", context.Identity.Name);
    }

    [Fact]
    public void Identity_WithNullPrincipal_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var assertion = new Assertion("testuser");
        var properties = new AuthenticationProperties();
        var backchannel = new HttpClient();

        // Act
        var context = new CasCreatingTicketContext(null!, properties, httpContext, scheme, options, backchannel, assertion);

        // Assert
        Assert.Null(context.Identity);
    }

    [Fact]
    public void Assertion_ShouldContainPrincipalName()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var assertion = new Assertion("testuser");
        var principal = new ClaimsPrincipal();
        var properties = new AuthenticationProperties();
        var backchannel = new HttpClient();

        // Act
        var context = new CasCreatingTicketContext(principal, properties, httpContext, scheme, options, backchannel, assertion);

        // Assert
        Assert.Equal("testuser", context.Assertion.PrincipalName);
    }

    [Fact]
    public void Backchannel_ShouldBeUsableForHttpRequests()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var assertion = new Assertion("testuser");
        var principal = new ClaimsPrincipal();
        var properties = new AuthenticationProperties();
        var backchannel = new HttpClient();

        // Act
        var context = new CasCreatingTicketContext(principal, properties, httpContext, scheme, options, backchannel, assertion);

        // Assert
        Assert.NotNull(context.Backchannel);
        Assert.IsType<HttpClient>(context.Backchannel);
    }

    [Fact]
    public void Properties_ShouldBeAccessible()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var assertion = new Assertion("testuser");
        var principal = new ClaimsPrincipal();
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            RedirectUri = "/home"
        };
        var backchannel = new HttpClient();

        // Act
        var context = new CasCreatingTicketContext(principal, properties, httpContext, scheme, options, backchannel, assertion);

        // Assert
        Assert.NotNull(context.Properties);
        Assert.True(context.Properties.IsPersistent);
        Assert.Equal("/home", context.Properties.RedirectUri);
    }
}
