using System;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

/// <summary>
/// Tests for CasAuthenticationOptions configuration class
/// </summary>
public class CasAuthenticationOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var options = new CasAuthenticationOptions();

        // Assert
        Assert.Equal("/signin-cas", options.CallbackPath.Value);
        Assert.Equal("/signout-callback-cas", options.SignedOutCallbackPath.Value);
        Assert.Equal("/", options.SignedOutRedirectUri);
        Assert.NotNull(options.Events);
        Assert.IsType<CasEvents>(options.Events);
    }

    [Fact]
    public void AuthenticationType_ShouldReturnCasDefaultAuthenticationType()
    {
        // Arrange
        var options = new CasAuthenticationOptions();

        // Act
        var authType = options.AuthenticationType;

        // Assert
        Assert.Equal(CasDefaults.AuthenticationType, authType);
    }

    [Fact]
    public void CasServerUrlBase_ShouldBeSettable()
    {
        // Arrange
        var options = new CasAuthenticationOptions();
        var serverUrl = "https://cas.example.com/cas";

        // Act
        options.CasServerUrlBase = serverUrl;

        // Assert
        Assert.Equal(serverUrl, options.CasServerUrlBase);
    }

    [Fact]
    public void SignedOutCallbackPath_ShouldBeSettable()
    {
        // Arrange
        var options = new CasAuthenticationOptions();
        var callbackPath = "/custom-signout-callback";

        // Act
        options.SignedOutCallbackPath = callbackPath;

        // Assert
        Assert.Equal(callbackPath, options.SignedOutCallbackPath.Value);
    }

    [Fact]
    public void SignedOutRedirectUri_ShouldBeSettable()
    {
        // Arrange
        var options = new CasAuthenticationOptions();
        var redirectUri = "/logout-success";

        // Act
        options.SignedOutRedirectUri = redirectUri;

        // Assert
        Assert.Equal(redirectUri, options.SignedOutRedirectUri);
    }

    [Fact]
    public void Events_ShouldBeSettable()
    {
        // Arrange
        var options = new CasAuthenticationOptions();
        var customEvents = new CasEvents();

        // Act
        options.Events = customEvents;

        // Assert
        Assert.Same(customEvents, options.Events);
    }

    [Fact]
    public void CasAuthenticationOptions_ShouldImplementICasOptions()
    {
        // Arrange & Act
        var options = new CasAuthenticationOptions();

        // Assert
        Assert.IsAssignableFrom<ICasOptions>(options);
    }

    [Fact]
    public void CasAuthenticationOptions_ShouldInheritFromRemoteAuthenticationOptions()
    {
        // Arrange & Act
        var options = new CasAuthenticationOptions();

        // Assert
        Assert.IsAssignableFrom<RemoteAuthenticationOptions>(options);
    }

    [Fact]
    public void Validate_ShouldThrowArgumentException_WhenCasServerUrlBaseIsNull()
    {
        // Arrange
        var options = new CasAuthenticationOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("CasServerUrlBase", exception.Message);
    }

    [Fact]
    public void Validate_ShouldThrowArgumentException_WhenCasServerUrlBaseIsEmpty()
    {
        // Arrange
        var options = new CasAuthenticationOptions
        {
            CasServerUrlBase = string.Empty
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("CasServerUrlBase", exception.Message);
    }

    [Fact]
    public void Validate_ShouldThrowArgumentException_WhenCasServerUrlBaseIsWhitespace()
    {
        // Arrange
        var options = new CasAuthenticationOptions
        {
            CasServerUrlBase = "   "
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("CasServerUrlBase", exception.Message);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenCasServerUrlBaseIsValid()
    {
        // Arrange
        var options = new CasAuthenticationOptions
        {
            CasServerUrlBase = "https://cas.example.com/cas"
        };

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => options.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void CasAuthenticationOptions_ShouldAllowCompleteConfiguration()
    {
        // Arrange
        var serverUrl = "https://cas.example.com/cas";
        var callbackPath = "/custom-signin";
        var signedOutCallbackPath = "/custom-signout";
        var signedOutRedirectUri = "/goodbye";

        // Act
        var options = new CasAuthenticationOptions
        {
            CasServerUrlBase = serverUrl,
            CallbackPath = callbackPath,
            SignedOutCallbackPath = signedOutCallbackPath,
            SignedOutRedirectUri = signedOutRedirectUri
        };

        // Assert
        Assert.Equal(serverUrl, options.CasServerUrlBase);
        Assert.Equal(callbackPath, options.CallbackPath.Value);
        Assert.Equal(signedOutCallbackPath, options.SignedOutCallbackPath.Value);
        Assert.Equal(signedOutRedirectUri, options.SignedOutRedirectUri);
    }
}
