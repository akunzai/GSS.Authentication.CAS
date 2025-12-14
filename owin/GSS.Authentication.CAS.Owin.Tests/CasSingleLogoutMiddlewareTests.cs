using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Owin.Testing;
using NSubstitute;
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
            var cache = Substitute.For<IDistributedCache>();
            using var server = CreateServer(cache);
            using var content = new StringContent("TEST");
            content.Headers.ContentType = null;

            // Act
            using var response = await server.HttpClient.PostAsync("/", content, TestContext.Current.CancellationToken);

            // Assert
            await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task WithJsonLogoutRequest_ShouldNotRemoveTicket()
        {
            // Arrange
            var cache = Substitute.For<IDistributedCache>();
            using var server = CreateServer(cache);
            var content = new StringContent(
                JsonSerializer.Serialize(new { logoutRequest = new { ticket = Guid.NewGuid().ToString() } }),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            using var response = await server.HttpClient.PostAsync("/", content, TestContext.Current.CancellationToken);

            // Assert
            await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task WithFormUrlEncodedLogoutRequest_ShouldRemoveTicket()
        {
            // Arrange
            var cache = Substitute.For<IDistributedCache>();
            using var server = CreateServer(cache);
            var removedTicket = string.Empty;
            cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask)
                .AndDoes(x => removedTicket = x.Arg<string>());
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
            await cache.Received(1).RemoveAsync(_options.CacheKeyFactory(ticket), Arg.Any<CancellationToken>());
        }

        private TestServer CreateServer(IDistributedCache cache)
        {
            return TestServer.Create(app =>
                app.UseCasSingleLogout(new DistributedCacheIAuthenticationSessionStore(cache,
                    Options.Create(_options))));
        }
    }
}
