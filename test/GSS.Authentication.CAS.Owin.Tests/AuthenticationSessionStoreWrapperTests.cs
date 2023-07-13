using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Owin.Security;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests;

public class AuthenticationSessionStoreWrapperTests
{
    [Fact]
    public async Task StoreGenericPrincipal_ShouldNotThrows()
    {
        // Arrange
        var serviceTickets = new Mock<IServiceTicketStore>();
        serviceTickets.Setup(x => x.StoreAsync(It.IsAny<ServiceTicket>()))
            .Returns<ServiceTicket>(ticket => Task.FromResult(ticket.TicketId)).Verifiable();
        var tickets = new AuthenticationSessionStoreWrapper(serviceTickets.Object);
        var authenticationTicket =
            new AuthenticationTicket(new GenericIdentity("TEST", "TEST"), null);

        // Act
        var actual = await tickets.StoreAsync(authenticationTicket);

        // Assert
        Assert.NotNull(actual);
        serviceTickets.Verify();
    }

    [Fact]
    public async Task StoreClaimsIdentity_ShouldNotThrows()
    {
        // Arrange
        var serviceTickets = new Mock<IServiceTicketStore>();
        serviceTickets.Setup(x => x.StoreAsync(It.IsAny<ServiceTicket>()))
            .Returns<ServiceTicket>(ticket => Task.FromResult(ticket.TicketId)).Verifiable();
        var tickets = new AuthenticationSessionStoreWrapper(serviceTickets.Object);
        var authenticationTicket = new AuthenticationTicket(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Guid.NewGuid().ToString()) }, "TEST"), null);

        // Act
        var actual = await tickets.StoreAsync(authenticationTicket);

        // Assert
        Assert.NotNull(actual);
        serviceTickets.Verify();
    }

    [Fact]
    public async Task StoreClaimsPrincipalWithServiceTicket_ShouldReturnAsKey()
    {
        // Arrange
        var expected = Guid.NewGuid().ToString();
        var serviceTickets = new Mock<IServiceTicketStore>();
        serviceTickets.Setup(x => x.StoreAsync(It.IsAny<ServiceTicket>()))
            .ReturnsAsync(expected).Verifiable();
        var tickets = new AuthenticationSessionStoreWrapper(serviceTickets.Object);
        var authenticationTicket = new AuthenticationTicket(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Guid.NewGuid().ToString()) }, "TEST"), null);
        authenticationTicket.Properties.SetServiceTicket(expected);

        // Act
        var actual = await tickets.StoreAsync(authenticationTicket);

        // Assert
        Assert.Equal(expected, actual);
        serviceTickets.Verify();
    }

    [Fact]
    public async Task RetrieveInvalidKey_ShouldReturnNull()
    {
        // Arrange
        var serviceTickets = new Mock<IServiceTicketStore>();
        serviceTickets.Setup(x => x.RetrieveAsync(It.IsAny<string>()))
            .ReturnsAsync((ServiceTicket?)null).Verifiable();
        var tickets = new AuthenticationSessionStoreWrapper(serviceTickets.Object);

        // Act
        var actual = await tickets.RetrieveAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Null(actual);
        serviceTickets.Verify();
    }

    [Fact]
    public async Task RetrieveValidKey_ShouldReturnAuthenticationTicket()
    {
        // Arrange
        var serviceTickets = new Mock<IServiceTicketStore>();
        var store = new Dictionary<string, ServiceTicket>();
        serviceTickets.Setup(x => x.StoreAsync(It.IsAny<ServiceTicket>()))
            .Callback<ServiceTicket>(x => store[x.TicketId] = x)
            .Returns<ServiceTicket>(ticket => Task.FromResult(ticket.TicketId)).Verifiable();
        serviceTickets.Setup(x => x.RetrieveAsync(It.IsAny<string>()))
            .Returns<string>(key => Task.FromResult(store.TryGetValue(key, out var ticket) ? ticket : null))
            .Verifiable();
        var tickets = new AuthenticationSessionStoreWrapper(serviceTickets.Object);
        var expected = new AuthenticationTicket(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "TEST")
                }, "TEST", ClaimTypes.NameIdentifier, ClaimTypes.Role), null);
        var key = await tickets.StoreAsync(expected);

        // Act
        var actual = await tickets.RetrieveAsync(key);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected.Identity.AuthenticationType, actual.Identity.AuthenticationType);
        Assert.Equal(expected.Identity.GetPrincipalName(), actual.Identity.GetPrincipalName());
        Assert.Equal(expected.Identity.Claims.First(c=>c.Type.Equals(expected.Identity.RoleClaimType)).Value, actual.Identity.Claims.First(c=>c.Type.Equals(actual.Identity.RoleClaimType)).Value);
        Assert.Equal(expected.Identity.NameClaimType, actual.Identity.NameClaimType);
        Assert.Equal(expected.Identity.RoleClaimType, actual.Identity.RoleClaimType);
        serviceTickets.Verify();
    }

    [Fact]
    public async Task RenewAuthenticationTicket_ShouldReplaceIt()
    {
        // Arrange
        var serviceTickets = new Mock<IServiceTicketStore>();
        var store = new Dictionary<string, ServiceTicket>();
        serviceTickets.Setup(x => x.StoreAsync(It.IsAny<ServiceTicket>()))
            .Callback<ServiceTicket>(x => store[x.TicketId] = x)
            .Returns<ServiceTicket>(ticket => Task.FromResult(ticket.TicketId)).Verifiable();
        serviceTickets.Setup(x => x.RenewAsync(It.IsAny<string>(), It.IsAny<ServiceTicket>()))
            .Callback<string, ServiceTicket>((key, x) => store[key] = x).Returns(Task.CompletedTask).Verifiable();
        serviceTickets.Setup(x => x.RetrieveAsync(It.IsAny<string>()))
            .Returns<string>(key => Task.FromResult(store.TryGetValue(key, out var ticket) ? ticket : null))
            .Verifiable();
        var tickets = new AuthenticationSessionStoreWrapper(serviceTickets.Object);
        var key = await tickets.StoreAsync(
            new AuthenticationTicket(new GenericIdentity("TEST", "OLD"), null));
        var expected = new AuthenticationTicket(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Guid.NewGuid().ToString()) }, "NEW"), null);

        // Act
        await tickets.RenewAsync(key, expected);

        // Assert
        var actual = await tickets.RetrieveAsync(key);
        Assert.NotNull(actual);
        Assert.Equal(expected.Identity.AuthenticationType, actual.Identity.AuthenticationType);
        Assert.Equal(expected.Identity.GetPrincipalName(), actual.Identity.GetPrincipalName());
        serviceTickets.Verify();
    }

    [Fact]
    public async Task RemoveValidKey_ShouldSuccess()
    {
        // Arrange
        var serviceTickets = new Mock<IServiceTicketStore>();
        // ReSharper disable once CollectionNeverQueried.Local
        var store = new Dictionary<string, ServiceTicket>();
        serviceTickets.Setup(x => x.StoreAsync(It.IsAny<ServiceTicket>()))
            .Callback<ServiceTicket>(x => store[x.TicketId] = x)
            .Returns<ServiceTicket>(ticket => Task.FromResult(ticket.TicketId)).Verifiable();
        serviceTickets.Setup(x => x.RemoveAsync(It.IsAny<string>()))
            .Callback<string>(key => store.Remove(key))
            .Returns(Task.CompletedTask).Verifiable();
        var tickets = new AuthenticationSessionStoreWrapper(serviceTickets.Object);
        var key = await tickets.StoreAsync(new AuthenticationTicket(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Guid.NewGuid().ToString()) }, "TEST"), null));

        // Act
        await tickets.RemoveAsync(key);

        // Assert
        var actual = await tickets.RetrieveAsync(key);
        Assert.Null(actual);
        serviceTickets.Verify();
    }
}