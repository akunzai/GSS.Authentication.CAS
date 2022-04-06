using System.Security.Claims;
using GSS.Authentication.CAS.Security;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace GSS.Authentication.CAS.DistributedCache.Tests;

public class DistributedCacheServiceTicketStoreTests
{
    private static readonly IDistributedCache _cache =
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private readonly IServiceTicketStore _serviceTickets;

    public DistributedCacheServiceTicketStoreTests()
    {
        _serviceTickets = new DistributedCacheServiceTicketStore(_cache);
    }

    [Fact]
    public async Task StoreServiceTicketSuccess_ShouldReturnNonEmptyKey()
    {
        // Arrange
        var expected = GenerateNewServiceTicket();

        // Act
        var key = await _serviceTickets.StoreAsync(expected).ConfigureAwait(false);

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
        await _serviceTickets.RenewAsync(key, expected).ConfigureAwait(false);

        // Assert
        var ignored = await _serviceTickets.RetrieveAsync(expected.TicketId).ConfigureAwait(false);
        Assert.Null(ignored);
        var actual = await _serviceTickets.RetrieveAsync(key).ConfigureAwait(false);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task RenewServiceTicketWithExistKey_ShouldNotRemoveExistEntry()
    {
        // Arrange
        var existEntry = GenerateNewServiceTicket();
        var key = await _serviceTickets.StoreAsync(existEntry).ConfigureAwait(false);
        var newEntry = GenerateNewServiceTicket();

        // Act
        await _serviceTickets.RenewAsync(key, newEntry).ConfigureAwait(false);

        // Assert
        var exist = await _serviceTickets.RetrieveAsync(existEntry.TicketId).ConfigureAwait(false);
        Assert.NotNull(exist);
        var actual = await _serviceTickets.RetrieveAsync(key).ConfigureAwait(false);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task RetrieveServiceTicketWithExistKey_ShouldReturnEntry()
    {
        // Arrange
        var expected = GenerateNewServiceTicket();
        var key = await _serviceTickets.StoreAsync(expected).ConfigureAwait(false);

        // Act
        var actual = await _serviceTickets.RetrieveAsync(key).ConfigureAwait(false);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected.TicketId, actual!.TicketId);
        Assert.Equal(expected.AuthenticationType, actual.AuthenticationType);
        Assert.Equal(expected.Claims.First(x => x.Type == ClaimTypes.Name).Value,
            actual.Claims.First(x => x.Type == ClaimTypes.Name).Value);
        Assert.Equal(expected.Assertion.PrincipalName, actual.Assertion.PrincipalName);
        Assert.Equal(expected.Assertion.Attributes, actual.Assertion.Attributes);
        Assert.Equal(expected.Assertion.ValidFrom, actual.Assertion.ValidFrom);
        Assert.Equal(expected.Assertion.ValidUntil, actual.Assertion.ValidUntil);
    }

    [Fact]
    public async Task RemoveServiceTicketWithExistKey_ShouldRemoveEntry()
    {
        // Arrange
        var expected = GenerateNewServiceTicket();
        var key = await _serviceTickets.StoreAsync(expected).ConfigureAwait(false);

        // Act
        await _serviceTickets.RemoveAsync(key).ConfigureAwait(false);

        // Assert
        var actual = await _serviceTickets.RetrieveAsync(key).ConfigureAwait(false);
        Assert.Null(actual);
    }

    private static ServiceTicket GenerateNewServiceTicket(Action<ServiceTicket>? setupAction = null)
    {
        var ticket = new ServiceTicket(Guid.NewGuid().ToString(),
            new Assertion("test",
                new Dictionary<string, StringValues> { ["foo"] = "bar" },
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1)),
            new List<Claim> { new Claim(ClaimTypes.Name, Guid.NewGuid().ToString()) }, "TEST");
        setupAction?.Invoke(ticket);
        return ticket;
    }
}