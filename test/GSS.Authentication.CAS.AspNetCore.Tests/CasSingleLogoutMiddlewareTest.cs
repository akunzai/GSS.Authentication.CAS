using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Testing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests
{
    public class CasSingleLogoutMiddlewareTest : IClassFixture<CasFixture>
    {
        private readonly IFileProvider _files;

        public CasSingleLogoutMiddlewareTest(CasFixture fixture)
        {
            _files = fixture.FileProvider;
        }

        [Fact]
        public async Task WithoutLogoutRequest_ShouldNotRemoveTicket()
        {
            // Arrange
            var store = new Mock<ITicketStore>();
            using var server = CreateServer(store.Object);
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
            using var server = CreateServer(store.Object);
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
            using var server = CreateServer(store.Object);
            using var client = server.CreateClient();
            var ticket = Guid.NewGuid().ToString();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logoutRequest"] = _files.ReadAsString("SamlLogoutRequest.xml").Replace("$TICKET", ticket)
            });

            // Act
            using var response = await client.PostAsync("/", content).ConfigureAwait(false);

            // Assert
            Assert.Equal(ticket, removedTicket);
            store.Verify(x => x.RemoveAsync(ticket), Times.Once);
        }

        private static TestServer CreateServer(ITicketStore store)
        {
            return new TestServer(new WebHostBuilder()
                .Configure(app => app.UseCasSingleLogout(store)));
        }
    }
}
