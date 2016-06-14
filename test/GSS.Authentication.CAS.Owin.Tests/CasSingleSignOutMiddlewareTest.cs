using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Moq;
using Owin;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests
{
    public class CasSingleSignOutMiddlewareTest : IDisposable
    {
        protected IServiceTicketStore store;
        protected TestServer server;
        protected HttpClient client;
        public CasSingleSignOutMiddlewareTest()
        {
            // Arrange
            store = Mock.Of<IServiceTicketStore>();
            server = TestServer.Create(app =>
            {
                app.UseCasSingleSignOut(new AuthenticationSessionStoreWrapper(store));
            });
            client = server.HttpClient;
        }

        public void Dispose()
        {
            server.Dispose();
        }

        [Trait("pass", "true")]
        [Fact]
        public async Task RecievedSignoutRequest_FailAsync()
        {
            // Act
            var response = await client.PostAsync("/", new StringContent(string.Empty));

            // Assert
            Mock.Get(store).Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Trait("pass", "true")]
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
