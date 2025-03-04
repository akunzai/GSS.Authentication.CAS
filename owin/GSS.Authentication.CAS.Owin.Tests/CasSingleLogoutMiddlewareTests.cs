using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Owin.Testing;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests
{
    public class CasSingleLogoutMiddlewareTests
    {
        private readonly DistributedCacheIAuthenticationSessionStoreOptions _options =
            new DistributedCacheIAuthenticationSessionStoreOptions();

        [Fact]
        public async Task WithoutLogoutRequest_ShouldNotRemoveTicket()
        {
            // Arrange
            var cache = new Mock<IDistributedCache>();
            using var server = CreateServer(cache.Object);
            using var content = new StringContent("TEST");
            content.Headers.ContentType = null;

            // Act
            using var response = await server.HttpClient.PostAsync("/", content, TestContext.Current.CancellationToken);

            // Assert
            cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithJsonLogoutRequest_ShouldNotRemoveTicket()
        {
            // Arrange
            var cache = new Mock<IDistributedCache>();
            using var server = CreateServer(cache.Object);
            var content = new StringContent(
                JsonSerializer.Serialize(new { logoutRequest = new { ticket = Guid.NewGuid().ToString() } }),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            using var response = await server.HttpClient.PostAsync("/", content, TestContext.Current.CancellationToken);

            // Assert
            cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithFormUrlEncodedLogoutRequest_ShouldRemoveTicket()
        {
            // Arrange
            var cache = new Mock<IDistributedCache>();
            using var server = CreateServer(cache.Object);
            var removedTicket = string.Empty;
            cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((x, _) => removedTicket = x)
                .Returns(Task.CompletedTask);
            var ticket = Guid.NewGuid().ToString();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logoutRequest"] =
                    $@"<samlp:LogoutRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" ID=""{Guid.NewGuid()}"" Version=""2.0"" IssueInstant=""{DateTime.UtcNow:o}"">
    <saml:NameID xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"">@NOT_USED@</saml:NameID>
    <samlp:SessionIndex>{ticket}</samlp:SessionIndex></samlp:LogoutRequest>"
            });

            // Act
            using var response = await server.HttpClient.PostAsync("/", content, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(_options.CacheKeyFactory(ticket), removedTicket);
            cache.Verify(x => x.RemoveAsync(_options.CacheKeyFactory(ticket), It.IsAny<CancellationToken>()), Times.Once);
        }

        private TestServer CreateServer(IDistributedCache cache)
        {
            return TestServer.Create(app =>
                app.UseCasSingleLogout(new DistributedCacheIAuthenticationSessionStore(cache,
                    Options.Create(_options))));
        }
    }
}