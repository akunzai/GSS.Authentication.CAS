using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

public class CasSingleLogoutMiddlewareTests
{
    private readonly DistributedCacheTicketStoreOptions _options = new();

    [Fact]
    public async Task WithoutLogoutRequest_ShouldNotRemoveTicket()
    {
        // Arrange
        var cache = new Mock<IDistributedCache>();
        using var host = CreateHost(cache.Object);
        var server = host.GetTestServer();
        await host.StartAsync();
        using var client = server.CreateClient();
        using var content = new StringContent("TEST");
        content.Headers.ContentType = null;

        // Act
        using var response = await client.PostAsync("/", content);

        // Assert
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithJsonLogoutRequest_ShouldNotRemoveTicket()
    {
        // Arrange
        var cache = new Mock<IDistributedCache>();
        using var host = CreateHost(cache.Object);
        var server = host.GetTestServer();
        await host.StartAsync();
        using var client = server.CreateClient();
        using var content = new StringContent(
            JsonSerializer.Serialize(new { logoutRequest = new { ticket = Guid.NewGuid().ToString() } }),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        using var response = await client.PostAsync("/", content);

        // Assert
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithFormUrlEncodedLogoutRequest_ShouldRemoveTicket()
    {
        // Arrange
        var cache = new Mock<IDistributedCache>();
        var removedTicket = string.Empty;
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((x, _) => removedTicket = x)
            .Returns(Task.CompletedTask);
        using var host = CreateHost(cache.Object);
        var server = host.GetTestServer();
        await host.StartAsync();
        using var client = server.CreateClient();
        var ticket = Guid.NewGuid().ToString();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["logoutRequest"] =
                $@"<samlp:LogoutRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" ID=""{Guid.NewGuid()}"" Version=""2.0"" IssueInstant=""{DateTime.UtcNow:o}"">
    <saml:NameID xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"">@NOT_USED@</saml:NameID>
    <samlp:SessionIndex>{ticket}</samlp:SessionIndex></samlp:LogoutRequest>"
        });

        // Act
        using var response = await client.PostAsync("/", content);

        // Assert
        Assert.Equal(_options.CacheKeyFactory(ticket), removedTicket);
        cache.Verify(x => x.RemoveAsync(_options.CacheKeyFactory(ticket), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetrievedTicketStoreFromDI_ShouldNotThrows()
    {
        // Arrange
        var host = new HostBuilder()
            .ConfigureWebHostDefaults(builder =>
            {
                builder.UseTestServer();
                builder.ConfigureServices(services => services.AddSingleton(Mock.Of<ITicketStore>()));
                builder.Configure(app => app.UseCasSingleLogout());
            })
            .Build();

        // Act & Assert
        await host.StartAsync();
    }

    private IHost CreateHost(IDistributedCache cache)
    {
        return new HostBuilder()
            .ConfigureWebHostDefaults(builder =>
            {
                builder.UseTestServer();
                builder.Configure(app =>
                    app.UseCasSingleLogout(new DistributedCacheTicketStore(cache, Options.Create(_options))));
            })
            .Build();
    }
}