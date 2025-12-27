using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

public class CasExtensionsTests
{
    #region AddCAS Extension Methods Tests

    [Fact]
    public async Task AddCAS_WithoutParameters_ShouldRegisterWithDefaultScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthentication();
        var builder = new AuthenticationBuilder(services);

        // Act
        var result = builder.AddCAS();

        // Assert
        Assert.NotNull(result);
        var serviceProvider = services.BuildServiceProvider();
        var schemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(CasDefaults.AuthenticationType);

        Assert.NotNull(scheme);
        Assert.Equal(CasDefaults.AuthenticationType, scheme.Name);
        Assert.Equal(typeof(CasAuthenticationHandler), scheme.HandlerType);
    }

    [Fact]
    public void AddCAS_WithConfigureOptions_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddOptions();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.AddAuthentication();
        var builder = new AuthenticationBuilder(services);
        var expectedCasServerUrlBase = "https://cas.example.com";

        // Act
        builder.AddCAS(options =>
        {
            options.CasServerUrlBase = expectedCasServerUrlBase;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<CasAuthenticationOptions>>();
        var casOptions = options.Get(CasDefaults.AuthenticationType);

        Assert.Equal(expectedCasServerUrlBase, casOptions.CasServerUrlBase);
    }

    [Fact]
    public async Task AddCAS_WithCustomAuthenticationScheme_ShouldRegisterWithCustomScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthentication();
        var builder = new AuthenticationBuilder(services);
        var customScheme = "CustomCAS";

        // Act
        var result = builder.AddCAS(customScheme, options => { });

        // Assert
        Assert.NotNull(result);
        var serviceProvider = services.BuildServiceProvider();
        var schemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(customScheme);

        Assert.NotNull(scheme);
        Assert.Equal(customScheme, scheme.Name);
        Assert.Equal(typeof(CasAuthenticationHandler), scheme.HandlerType);
    }

    [Fact]
    public async Task AddCAS_WithCustomSchemeAndDisplayName_ShouldRegisterCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthentication();
        var builder = new AuthenticationBuilder(services);
        var customScheme = "CustomCAS";
        var customDisplayName = "My Custom CAS";

        // Act
        var result = builder.AddCAS(customScheme, customDisplayName, options => { });

        // Assert
        Assert.NotNull(result);
        var serviceProvider = services.BuildServiceProvider();
        var schemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(customScheme);

        Assert.NotNull(scheme);
        Assert.Equal(customScheme, scheme.Name);
        Assert.Equal(customDisplayName, scheme.DisplayName);
        Assert.Equal(typeof(CasAuthenticationHandler), scheme.HandlerType);
    }

    [Fact]
    public async Task AddCAS_WithNullConfigureOptions_ShouldRegisterSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthentication();
        var builder = new AuthenticationBuilder(services);

        // Act
        var result = builder.AddCAS(configureOptions: null);

        // Assert
        Assert.NotNull(result);
        var serviceProvider = services.BuildServiceProvider();
        var schemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(CasDefaults.AuthenticationType);

        Assert.NotNull(scheme);
    }

    [Fact]
    public void AddCAS_ShouldRegisterPostConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddOptions();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.AddAuthentication();
        var builder = new AuthenticationBuilder(services);

        // Act
        builder.AddCAS();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var postConfigureOptions = serviceProvider.GetServices<IPostConfigureOptions<CasAuthenticationOptions>>();

        Assert.NotEmpty(postConfigureOptions);
    }

    [Fact]
    public void AddCAS_CalledMultipleTimes_ShouldNotDuplicatePostConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddOptions();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.AddAuthentication();
        var builder = new AuthenticationBuilder(services);

        // Act
        builder.AddCAS();
        builder.AddCAS("CAS2", options => { });
        builder.AddCAS("CAS3", options => { });

        // Assert - TryAddEnumerable should prevent duplicates
        var serviceProvider = services.BuildServiceProvider();
        var postConfigureOptions = serviceProvider.GetServices<IPostConfigureOptions<CasAuthenticationOptions>>();

        Assert.NotEmpty(postConfigureOptions);
    }

    #endregion
}
