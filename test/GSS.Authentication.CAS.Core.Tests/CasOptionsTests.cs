using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

/// <summary>
/// Tests for CasOptions configuration class
/// </summary>
public class CasOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultAuthenticationType()
    {
        // Arrange & Act
        var options = new CasOptions();

        // Assert
        Assert.Equal(CasDefaults.AuthenticationType, options.AuthenticationType);
    }

    [Fact]
    public void AuthenticationType_ShouldBeSettable()
    {
        // Arrange
        var options = new CasOptions();
        var customAuthType = "CustomCAS";

        // Act
        options.AuthenticationType = customAuthType;

        // Assert
        Assert.Equal(customAuthType, options.AuthenticationType);
    }

    [Fact]
    public void CasServerUrlBase_ShouldBeSettable()
    {
        // Arrange
        var options = new CasOptions();
        var serverUrl = "https://cas.example.com/cas";

        // Act
        options.CasServerUrlBase = serverUrl;

        // Assert
        Assert.Equal(serverUrl, options.CasServerUrlBase);
    }

    [Fact]
    public void CasOptions_ShouldImplementICasOptions()
    {
        // Arrange & Act
        var options = new CasOptions();

        // Assert
        Assert.IsAssignableFrom<ICasOptions>(options);
    }

    [Fact]
    public void CasOptions_ShouldAllowCompleteConfiguration()
    {
        // Arrange
        var serverUrl = "https://cas.example.com/cas";
        var authType = "MyCAS";

        // Act
        var options = new CasOptions
        {
            CasServerUrlBase = serverUrl,
            AuthenticationType = authType
        };

        // Assert
        Assert.Equal(serverUrl, options.CasServerUrlBase);
        Assert.Equal(authType, options.AuthenticationType);
    }
}
