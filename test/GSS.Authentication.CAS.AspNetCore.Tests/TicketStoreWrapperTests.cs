using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Moq;
using Xunit;

namespace GSS.Authentication.CAS.AspNetCore.Tests
{
    public class TicketStoreWrapperTests
    {
        [Fact]
        public async Task StoreGenericPrincipal_ShouldNotThrows()
        {
            // Arrange
            var serviceTickets = new Mock<IServiceTicketStore>();
            serviceTickets.Setup(x => x.StoreAsync(It.IsAny<ServiceTicket>()))
                .Returns<ServiceTicket>(ticket => Task.FromResult(ticket.TicketId)).Verifiable();
            var tickets = new TicketStoreWrapper(serviceTickets.Object);
            var authenticationTicket =
                new AuthenticationTicket(new GenericPrincipal(new GenericIdentity("TEST"), new[] { "TEST" }), "TEST");

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
            var tickets = new TicketStoreWrapper(serviceTickets.Object);
            var authenticationTicket = new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, Guid.NewGuid().ToString())
                })), "TEST");

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
            var tickets = new TicketStoreWrapper(serviceTickets.Object);
            var authenticationTicket = new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, Guid.NewGuid().ToString())
                })), "TEST");
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
            var tickets = new TicketStoreWrapper(serviceTickets.Object);

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
            var tickets = new TicketStoreWrapper(serviceTickets.Object);
            var expected = new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "TEST")
                }, "TEST", ClaimTypes.NameIdentifier, ClaimTypes.Role)), "TEST");
            var key = await tickets.StoreAsync(expected);

            // Act
            var actual = await tickets.RetrieveAsync(key);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expected.AuthenticationScheme, actual?.AuthenticationScheme);
            Assert.Equal(expected.Principal.GetPrincipalName(), actual?.Principal.GetPrincipalName());
            Assert.True(actual?.Principal.IsInRole("TEST"));
            var expectedIdentity = expected.Principal.Identity as ClaimsIdentity;
            var actualIdentity = actual?.Principal.Identity as ClaimsIdentity;
            Assert.Equal(expectedIdentity?.NameClaimType, actualIdentity?.NameClaimType);
            Assert.Equal(expectedIdentity?.RoleClaimType, actualIdentity?.RoleClaimType);
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
            var tickets = new TicketStoreWrapper(serviceTickets.Object);
            var key = await tickets.StoreAsync(
                new AuthenticationTicket(new GenericPrincipal(new GenericIdentity("TEST"), new[] { "TEST" }), "OLD"));
            var expected = new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, Guid.NewGuid().ToString())
                })), "NEW");

            // Act
            await tickets.RenewAsync(key, expected);

            // Assert
            var actual = await tickets.RetrieveAsync(key);
            Assert.NotNull(actual);
            Assert.Equal(expected.AuthenticationScheme, actual?.AuthenticationScheme);
            Assert.Equal(expected.Principal.GetPrincipalName(), actual?.Principal.GetPrincipalName());
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
            var tickets = new TicketStoreWrapper(serviceTickets.Object);
            var key = await tickets.StoreAsync(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, Guid.NewGuid().ToString())
                })), "TEST"));

            // Act
            await tickets.RemoveAsync(key);

            // Assert
            var actual = await tickets.RetrieveAsync(key);
            Assert.Null(actual);
            serviceTickets.Verify();
        }
    }
}