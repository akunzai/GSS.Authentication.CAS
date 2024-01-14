using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests;

public class DistributedCacheServiceTicketStoreTests
{
    private static readonly IDistributedCache _cache =
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private readonly DistributedCacheTicketStore _ticketStore;
    private readonly DistributedCacheTicketStoreOptions _options;

    public DistributedCacheServiceTicketStoreTests()
    {
        _options = new DistributedCacheTicketStoreOptions();
        _ticketStore = new DistributedCacheTicketStore(_cache, Options.Create(_options));
    }

    [Fact]
    public async Task StoreWithServiceTicket_ShouldStoreItAsTicketId()
    {
        // Arrange
        var serviceTicket = Guid.NewGuid().ToString();
        var expected = GenerateNewTicket();
        expected.Properties.SetServiceTicket(serviceTicket);

        // Act
        var key = await _ticketStore.StoreAsync(expected);

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
        var key = await _ticketStore.StoreAsync(expected);

        // Assert
        Assert.NotNull(key);
        Assert.Equal(expected.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value, key);
    }

    [Fact]
    public async Task RenewWithNotExistKey_ShouldStoreNewEntry()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var expected = GenerateNewTicket();

        // Act
        await _ticketStore.RenewAsync(key, expected);

        // Assert
        var ignored = await _ticketStore.RetrieveAsync(_options.TicketIdFactory(expected));
        Assert.Null(ignored);
        var actual = await _ticketStore.RetrieveAsync(key);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task RenewWithExistKey_ShouldNotRemoveExistEntry()
    {
        // Arrange
        var existEntry = GenerateNewTicket();
        var key = await _ticketStore.StoreAsync(existEntry);
        var newEntry = GenerateNewTicket();

        // Act
        await _ticketStore.RenewAsync(key, newEntry);

        // Assert
        var exist = await _ticketStore.RetrieveAsync(_options.TicketIdFactory(existEntry));
        Assert.NotNull(exist);
        var actual = await _ticketStore.RetrieveAsync(key);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task RetrieveWithExistKey_ShouldReturnEntry()
    {
        // Arrange
        var expected = GenerateNewTicket();
        expected.Properties.IssuedUtc = DateTimeOffset.UtcNow;
        expected.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30);
        var key = await _ticketStore.StoreAsync(expected);

        // Act
        var actual = await _ticketStore.RetrieveAsync(key);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected.AuthenticationScheme, actual.AuthenticationScheme);
        Assert.Equal(_options.TicketIdFactory(expected), _options.TicketIdFactory(actual));
        Assert.Equal(expected.Principal.Identity?.Name,
            actual.Principal.Identity?.Name);
        Assert.Equal(expected.Properties.IssuedUtc, actual.Properties.IssuedUtc);
        Assert.Equal(expected.Properties.ExpiresUtc, actual.Properties.ExpiresUtc);
    }

    [Fact]
    public async Task RemoveWithExistKey_ShouldRemoveEntry()
    {
        // Arrange
        var expected = GenerateNewTicket();
        var key = await _ticketStore.StoreAsync(expected);

        // Act
        await _ticketStore.RemoveAsync(key);

        // Assert
        var actual = await _ticketStore.RetrieveAsync(key);
        Assert.Null(actual);
    }

    private static AuthenticationTicket GenerateNewTicket(Action<AuthenticationTicket>? setupAction = null)
    {
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, "TEST"),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                })), CasDefaults.AuthenticationType);
        setupAction?.Invoke(ticket);
        return ticket;
    }
}