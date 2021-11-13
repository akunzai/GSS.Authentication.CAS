using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

public class CasSingleLogoutMiddlewareTest
{
    [Fact]
    public async Task WithoutLogoutRequest_ShouldNotRemoveTicket()
    {
        // Arrange
        var store = new Mock<ITicketStore>();
        using var host = CreateHost(store.Object);
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();
        using var content = new StringContent("TEST");
        content.Headers.ContentType = null;

        // Act
        using var response = await client.PostAsync("/", content).ConfigureAwait(false);

        // Assert
        store.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WithJsonLogoutRequest_ShouldNotRemoveTicket()
    {
        // Arrange
        var store = new Mock<ITicketStore>();
        using var host = CreateHost(store.Object);
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();
        using var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                logoutRequest = new { ticket = Guid.NewGuid().ToString() }
            }),
            Encoding.UTF8,
            "application/json"
            );

        // Act
        using var response = await client.PostAsync("/", content).ConfigureAwait(false);

        // Assert
        store.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WithFormUrlEncodedLogoutRequest_ShouldRemoveTicket()
    {
        // Arrange
        var store = new Mock<ITicketStore>();
        var removedTicket = string.Empty;
        store.Setup(x => x.RemoveAsync(It.IsAny<string>()))
            .Callback<string>((x) => removedTicket = x)
            .Returns(Task.CompletedTask);
        using var host = CreateHost(store.Object);
        var server = host.GetTestServer();
        await host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();
        var ticket = Guid.NewGuid().ToString();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["logoutRequest"] = $@"<samlp:LogoutRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" ID=""{Guid.NewGuid()}"" Version=""2.0"" IssueInstant=""{DateTime.UtcNow:o}"">
    <saml:NameID xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"">@NOT_USED@</saml:NameID>
    <samlp:SessionIndex>{ticket}</samlp:SessionIndex></samlp:LogoutRequest>"
        });

        // Act
        using var response = await client.PostAsync("/", content).ConfigureAwait(false);

        // Assert
        Assert.Equal(ticket, removedTicket);
        store.Verify(x => x.RemoveAsync(ticket), Times.Once);
    }

    private static IHost CreateHost(ITicketStore store)
    {
        return new HostBuilder()
            .ConfigureWebHostDefaults(builder =>
            {
                builder.UseTestServer();
                builder.Configure(app => app.UseCasSingleLogout(store));
            })
            .Build();
    }
}