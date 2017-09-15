using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Tests;
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
        private readonly TestServer server;
        private readonly HttpClient client;
        private readonly CasFixture fixture;
        private ITicketStore store;

        public CasSingleSignOutMiddlewareTest(CasFixture fixture)
        {
            this.fixture = fixture;
            // Arrange
            store = Mock.Of<ITicketStore>();
            server = new TestServer(new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCasSingleSignOut(store);
                }));
            client = server.CreateClient();

        }
        public void Dispose()
        {
            server.Dispose();
        }

        [Fact]
        public async Task RecievedSignoutRequest_FailAsync()
        {
            // Arrange
            var content = new FormUrlEncodedContent(new Dictionary<string, string>());

            // Act
            await client.PostAsync("/", content);

            // Assert
            Mock.Get(store).Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RecievedSignoutRequest_SuccessAsync()
        {
            // Arrange
            var removedTicket = string.Empty;
            Mock.Get(store).Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Callback<string>((x) => removedTicket = x)
                .Returns(Task.CompletedTask);
            var ticket = Guid.NewGuid().ToString();
            var parts = new Dictionary<string, string>
            {
                ["logoutRequest"] = fixture.FileProvider.ReadAsString("SamlLogoutRequest.xml").Replace("$TICKET", ticket)
            };

            // Act
            await client.PostAsync("/", new FormUrlEncodedContent(parts));

            // Assert
            Assert.Equal(ticket, removedTicket);
            Mock.Get(store).Verify(x => x.RemoveAsync(ticket), Times.Once);
        }
    }
}
