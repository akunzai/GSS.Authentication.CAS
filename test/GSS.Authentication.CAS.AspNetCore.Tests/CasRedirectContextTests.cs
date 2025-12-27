using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

/// <summary>
/// Tests for CasRedirectContext
/// </summary>
public class CasRedirectContextTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties();
        var redirectUri = "https://cas.example.com/logout";

        // Act
        var context = new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);

        // Assert
        Assert.NotNull(context);
        Assert.Same(httpContext, context.HttpContext);
        Assert.Same(scheme, context.Scheme);
        Assert.Same(options, context.Options);
        Assert.Same(properties, context.Properties);
        Assert.Equal(redirectUri, context.RedirectUri);
    }

    [Fact]
    public void Handled_ShouldDefaultToFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties();
        var redirectUri = "https://cas.example.com/logout";

        // Act
        var context = new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);

        // Assert
        Assert.False(context.Handled);
    }

    [Fact]
    public void HandleResponse_ShouldSetHandledToTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties();
        var redirectUri = "https://cas.example.com/logout";
        var context = new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);

        // Act
        context.HandleResponse();

        // Assert
        Assert.True(context.Handled);
    }

    [Fact]
    public void HandleResponse_CalledMultipleTimes_ShouldKeepHandledTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties();
        var redirectUri = "https://cas.example.com/logout";
        var context = new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);

        // Act
        context.HandleResponse();
        context.HandleResponse();
        context.HandleResponse();

        // Assert
        Assert.True(context.Handled);
    }

    [Fact]
    public void RedirectUri_ShouldBeAccessible()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties();
        var redirectUri = "https://cas.example.com/logout?service=http://localhost";

        // Act
        var context = new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);

        // Assert
        Assert.Equal(redirectUri, context.RedirectUri);
    }

    [Fact]
    public void Properties_ShouldBeAccessible()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/home",
            IsPersistent = true
        };
        var redirectUri = "https://cas.example.com/logout";

        // Act
        var context = new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);

        // Assert
        Assert.NotNull(context.Properties);
        Assert.Equal("/home", context.Properties.RedirectUri);
        Assert.True(context.Properties.IsPersistent);
    }

    [Fact]
    public void Context_ShouldInheritFromRedirectContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties();
        var redirectUri = "https://cas.example.com/logout";

        // Act
        var context = new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);

        // Assert
        Assert.IsAssignableFrom<RedirectContext<CasAuthenticationOptions>>(context);
    }

    [Fact]
    public void Options_ShouldBeOfTypeCasAuthenticationOptions()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(CasDefaults.AuthenticationType, null, typeof(CasAuthenticationHandler));
        var options = new CasAuthenticationOptions { CasServerUrlBase = "https://cas.example.com" };
        var properties = new AuthenticationProperties();
        var redirectUri = "https://cas.example.com/logout";

        // Act
        var context = new CasRedirectContext(httpContext, scheme, options, properties, redirectUri);

        // Assert
        Assert.IsType<CasAuthenticationOptions>(context.Options);
        Assert.Equal("https://cas.example.com", context.Options.CasServerUrlBase);
    }
}
