using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Testing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests
{
    public class CasSingleSignOutMiddlewareTest : IClassFixture<CasFixture>, IDisposable
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly CasFixture _fixture;
        private readonly ITicketStore _store;

        public CasSingleSignOutMiddlewareTest(CasFixture fixture)
        {
            _fixture = fixture;
            // Arrange
            _store = Mock.Of<ITicketStore>();
            _server = new TestServer(new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCasSingleSignOut(_store);
                }));
            _client = _server.CreateClient();

        }
        public void Dispose()
        {
            _server.Dispose();
        }

        [Fact]
        public async Task RecievedSignoutRequest_FailAsync()
        {
            // Arrange
            var content = new FormUrlEncodedContent(new Dictionary<string, string>());

            // Act
            await _client.PostAsync("/", content).ConfigureAwait(false);

            // Assert
            Mock.Get(_store).Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RecievedSignoutRequest_FailWithJsonContentAsync()
        {
            // Arrange
            var content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    logoutRequest = new { ticket = Guid.NewGuid().ToString() }
                }),
                Encoding.UTF8,
                "application/json"
                );

            // Act
            await _client.PostAsync("/", content).ConfigureAwait(false);

            // Assert
            Mock.Get(_store).Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RecievedSignoutRequest_SuccessAsync()
        {
            // Arrange
            var removedTicket = string.Empty;
            Mock.Get(_store).Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Callback<string>((x) => removedTicket = x)
                .Returns(Task.CompletedTask);
            var ticket = Guid.NewGuid().ToString();
            var parts = new Dictionary<string, string>
            {
                ["logoutRequest"] = _fixture.FileProvider.ReadAsString("SamlLogoutRequest.xml").Replace("$TICKET", ticket)
            };

            // Act
            await _client.PostAsync("/", new FormUrlEncodedContent(parts)).ConfigureAwait(false);

            // Assert
            Assert.Equal(ticket, removedTicket);
            Mock.Get(_store).Verify(x => x.RemoveAsync(ticket), Times.Once);
        }
    }
}
