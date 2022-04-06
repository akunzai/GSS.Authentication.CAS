using System.Text;
using System.Text.Json;
using Microsoft.Owin.Testing;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests;

public class CasSingleLogoutMiddlewareTests
{
    [Fact]
    public async Task WithoutLogoutRequest_ShouldNotRemoveTicket()
    {
        // Arrange
        var store = new Mock<IServiceTicketStore>();
        using var server = CreateServer(store.Object);
        using var content = new StringContent("TEST");
        content.Headers.ContentType = null;

        // Act
        using var response = await server.HttpClient.PostAsync("/", content).ConfigureAwait(false);

        // Assert
        store.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WithJsonLogoutRequest_ShouldNotRemoveTicket()
    {
        // Arrange
        var store = new Mock<IServiceTicketStore>();
        using var server = CreateServer(store.Object);
        var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                logoutRequest = new { ticket = Guid.NewGuid().ToString() }
            }),
            Encoding.UTF8,
            "application/json"
            );

        // Act
        using var response = await server.HttpClient.PostAsync("/", content).ConfigureAwait(false);

        // Assert
        store.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WithFormUrlEncodedLogoutRequest_ShouldRemoveTicket()
    {
        // Arrange
        var store = new Mock<IServiceTicketStore>();
        using var server = CreateServer(store.Object);
        var removedTicket = string.Empty;
        store.Setup(x => x.RemoveAsync(It.IsAny<string>()))
            .Callback<string>((x) => removedTicket = x)
            .Returns(Task.CompletedTask);
        var ticket = Guid.NewGuid().ToString();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["logoutRequest"] = $@"<samlp:LogoutRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" ID=""{Guid.NewGuid()}"" Version=""2.0"" IssueInstant=""{DateTime.UtcNow:o}"">
    <saml:NameID xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"">@NOT_USED@</saml:NameID>
    <samlp:SessionIndex>{ticket}</samlp:SessionIndex></samlp:LogoutRequest>"
        });

        // Act
        using var response = await server.HttpClient.PostAsync("/", content).ConfigureAwait(false);

        // Assert
        Assert.Equal(ticket, removedTicket);
        store.Verify(x => x.RemoveAsync(ticket), Times.Once);
    }

    private static TestServer CreateServer(IServiceTicketStore store)
    {
        return TestServer.Create(app => app.UseCasSingleLogout(new AuthenticationSessionStoreWrapper(store)));
    }
}
