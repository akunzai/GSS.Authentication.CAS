using GSS.Authentication.CAS.Proxy;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class InMemoryProxyGrantingTicketStoreTests
{
    [Fact]
    public async Task StoreThenGet_ShouldReturnStoredProxyGrantingTicket()
    {
        // Arrange
        var store = new InMemoryProxyGrantingTicketStore();
        const string iou = "PGTIOU-1-abc123";
        const string pgt = "PGT-1-abc123";

        // Act
        await store.StoreAsync(iou, pgt, TestContext.Current.CancellationToken);
        var result = await store.GetAsync(iou, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(pgt, result);
    }

    [Fact]
    public async Task Get_WithUnknownIou_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryProxyGrantingTicketStore();

        // Act
        var result = await store.GetAsync("PGTIOU-unknown", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Remove_ShouldMakeSubsequentGetReturnNull()
    {
        // Arrange
        var store = new InMemoryProxyGrantingTicketStore();
        const string iou = "PGTIOU-1-abc123";
        await store.StoreAsync(iou, "PGT-1-abc123", TestContext.Current.CancellationToken);

        // Act
        await store.RemoveAsync(iou, TestContext.Current.CancellationToken);
        var result = await store.GetAsync(iou, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }
}
