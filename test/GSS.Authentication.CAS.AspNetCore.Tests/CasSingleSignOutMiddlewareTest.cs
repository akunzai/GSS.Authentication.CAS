using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Xunit;
using System.Threading.Tasks;

namespace GSS.Authentication.CAS.AspNetCore.Tests
{
    public class CasSingleSignOutMiddlewareTest : IDisposable
    {
        protected readonly TestServer server;
        protected readonly HttpClient client;
        protected ITicketStore store;

        public CasSingleSignOutMiddlewareTest()
        {
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
            var response = await client.PostAsync("/", content);

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
                .Returns(Task.FromResult(0));
            var ticket = Guid.NewGuid().ToString();
            var parts = new Dictionary<string, string>
            {
                ["logoutRequest"] = ResourceHelper.GetResourceString("Resources/SamlLogoutRequest.xml").Replace("$TICKET", ticket)
            };

            // Act
            var response = await client.PostAsync("/", new FormUrlEncodedContent(parts));

            // Assert
            Assert.Equal(ticket, removedTicket);
            Mock.Get(store).Verify(x => x.RemoveAsync(ticket), Times.Once);
        }
    }
}
