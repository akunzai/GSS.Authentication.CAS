using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Owin.Security;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests;

public class DistributedCacheIAuthenticationSessionStoreTests
{
    private static readonly IDistributedCache _cache =
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private readonly DistributedCacheIAuthenticationSessionStore _sessionStore;
    private readonly DistributedCacheIAuthenticationSessionStoreOptions _options;

    public DistributedCacheIAuthenticationSessionStoreTests()
    {
        _options = new DistributedCacheIAuthenticationSessionStoreOptions();
        _sessionStore = new DistributedCacheIAuthenticationSessionStore(_cache, Options.Create(_options));
    }

    [Fact]
    public async Task StoreWithServiceTicket_ShouldStoreItAsTicketId()
    {
        // Arrange
        var serviceTicket = Guid.NewGuid().ToString();
        var expected = GenerateNewTicket();
        expected.Properties.SetServiceTicket(serviceTicket);

        // Act
        var key = await _sessionStore.StoreAsync(expected);

        // Assert
        Assert.NotNull(key);
        Assert.Equal(serviceTicket, key);
    }

    [Fact]
    public async Task StoreWithoutServiceTicket_ShouldStoreNameIdAsTicketId()
    {
        // Arrange
        var expected = GenerateNewTicket();

        // Act
        var key = await _sessionStore.StoreAsync(expected);

        // Assert
        Assert.NotNull(key);
        Assert.Equal(expected.Identity.FindFirst(ClaimTypes.NameIdentifier)!.Value, key);
    }

    [Fact]
    public async Task RenewWithNotExistKey_ShouldStoreNewEntry()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var expected = GenerateNewTicket();

        // Act
        await _sessionStore.RenewAsync(key, expected);

        // Assert
        var ignored = await _sessionStore.RetrieveAsync(_options.TicketIdFactory(expected));
        Assert.Null(ignored);
        var actual = await _sessionStore.RetrieveAsync(key);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task RenewWithExistKey_ShouldNotRemoveExistEntry()
    {
        // Arrange
        var existEntry = GenerateNewTicket();
        var key = await _sessionStore.StoreAsync(existEntry);
        var newEntry = GenerateNewTicket();

        // Act
        await _sessionStore.RenewAsync(key, newEntry);

        // Assert
        var exist = await _sessionStore.RetrieveAsync(_options.TicketIdFactory(existEntry));
        Assert.NotNull(exist);
        var actual = await _sessionStore.RetrieveAsync(key);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task RetrieveWithExistKey_ShouldReturnEntry()
    {
        // Arrange
        var expected = GenerateNewTicket();
        expected.Properties.IssuedUtc = DateTimeOffset.UtcNow;
        expected.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30);
        var key = await _sessionStore.StoreAsync(expected);

        // Act
        var actual = await _sessionStore.RetrieveAsync(key);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected.Identity.AuthenticationType, actual.Identity.AuthenticationType);
        Assert.Equal(_options.TicketIdFactory(expected), _options.TicketIdFactory(actual));
        Assert.Equal(expected.Identity?.Name,
            actual.Identity?.Name);
        Assert.Equal(expected.Properties.IssuedUtc, actual.Properties.IssuedUtc);
        Assert.Equal(expected.Properties.ExpiresUtc, actual.Properties.ExpiresUtc);
    }

    [Fact]
    public async Task RemoveWithExistKey_ShouldRemoveEntry()
    {
        // Arrange
        var expected = GenerateNewTicket();
        var key = await _sessionStore.StoreAsync(expected);

        // Act
        await _sessionStore.RemoveAsync(key);

        // Assert
        var actual = await _sessionStore.RetrieveAsync(key);
        Assert.Null(actual);
    }

    private static AuthenticationTicket GenerateNewTicket(Action<AuthenticationTicket>? setupAction = null)
    {
        var ticket = new AuthenticationTicket(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, "TEST"),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                }),
            new AuthenticationProperties());
        setupAction?.Invoke(ticket);
        return ticket;
    }
}