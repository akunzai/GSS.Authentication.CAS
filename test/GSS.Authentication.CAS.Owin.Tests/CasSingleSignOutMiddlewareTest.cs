using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Testing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Owin.Testing;
using Moq;
using Owin;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests
{
    public class CasSingleSignOutMiddlewareTest : IClassFixture<CasFixture>
    {
        private readonly IFileProvider _files;

        public CasSingleSignOutMiddlewareTest(CasFixture fixture)
        {
            _files = fixture.FileProvider;
        }

        [Fact]
        public async Task WithoutLogoutRequest_ShouldNotRemoveTicket()
        {
            // Arrange
            var store = new Mock<IServiceTicketStore>();
            using var server = CreateServer(store.Object);

            // Act
            using var response = await server.HttpClient.PostAsync("/", new FormUrlEncodedContent(new Dictionary<string, string>())).ConfigureAwait(false);

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
                ["logoutRequest"] = _files.ReadAsString("SamlLogoutRequest.xml").Replace("$TICKET", ticket)
            });

            // Act
            using var response = await server.HttpClient.PostAsync("/", content).ConfigureAwait(false);

            // Assert
            Assert.Equal(ticket, removedTicket);
            store.Verify(x => x.RemoveAsync(ticket), Times.Once);
        }

        private TestServer CreateServer(IServiceTicketStore store)
        {
            return TestServer.Create(app => app.UseCasSingleSignOut(new AuthenticationSessionStoreWrapper(store)));
        }
    }
}
