using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace GSS.Authentication.CAS.DistributedCache.Tests;

public class DistributedCacheServiceTicketStoreTests
{
    private static readonly IDistributedCache _cache =
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private readonly IServiceTicketStore _serviceTickets;

    public DistributedCacheServiceTicketStoreTests()
    {
        _serviceTickets = new DistributedCacheServiceTicketStore(_cache,
            Options.Create(DistributedCacheServiceTicketStoreOptions.Default));
    }

    [Fact]
    public async Task StoreServiceTicketSuccess_ShouldReturnNonEmptyKey()
    {
        // Arrange
        var expected = GenerateNewServiceTicket();

        // Act
        var key = await _serviceTickets.StoreAsync(expected);

        // Assert
        Assert.NotNull(key);
    }

    [Fact]
    public async Task RenewServiceTicketWithNotExistKey_ShouldStoreNewEntry()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var expected = GenerateNewServiceTicket();

        // Act
        await _serviceTickets.RenewAsync(key, expected);

        // Assert
        var ignored = await _serviceTickets.RetrieveAsync(expected.TicketId);
        Assert.Null(ignored);
        var actual = await _serviceTickets.RetrieveAsync(key);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task RenewServiceTicketWithExistKey_ShouldNotRemoveExistEntry()
    {
        // Arrange
        var existEntry = GenerateNewServiceTicket();
        var key = await _serviceTickets.StoreAsync(existEntry);
        var newEntry = GenerateNewServiceTicket();

        // Act
        await _serviceTickets.RenewAsync(key, newEntry);

        // Assert
        var exist = await _serviceTickets.RetrieveAsync(existEntry.TicketId);
        Assert.NotNull(exist);
        var actual = await _serviceTickets.RetrieveAsync(key);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task RetrieveServiceTicketWithExistKey_ShouldReturnEntry()
    {
        // Arrange
        var expected = GenerateNewServiceTicket();
        var key = await _serviceTickets.StoreAsync(expected);

        // Act
        var actual = await _serviceTickets.RetrieveAsync(key);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected.TicketId, actual.TicketId);
        Assert.Equal(expected.AuthenticationType, actual.AuthenticationType);
        Assert.Equal(expected.Claims.First(x => x.Type == ClaimTypes.Name).Value,
            actual.Claims.First(x => x.Type == ClaimTypes.Name).Value);
        Assert.Equal(expected.IssuedUtc, actual.IssuedUtc);
        Assert.Equal(expected.ExpiresUtc, actual.ExpiresUtc);
    }

    [Fact]
    public async Task RemoveServiceTicketWithExistKey_ShouldRemoveEntry()
    {
        // Arrange
        var expected = GenerateNewServiceTicket();
        var key = await _serviceTickets.StoreAsync(expected);

        // Act
        await _serviceTickets.RemoveAsync(key);

        // Assert
        var actual = await _serviceTickets.RetrieveAsync(key);
        Assert.Null(actual);
    }

    private static ServiceTicket GenerateNewServiceTicket(Action<ServiceTicket>? setupAction = null)
    {
        var ticket = new ServiceTicket(Guid.NewGuid().ToString(),
            new List<Claim> { new(ClaimTypes.Name, Guid.NewGuid().ToString()) }, "TEST");
        setupAction?.Invoke(ticket);
        return ticket;
    }
}