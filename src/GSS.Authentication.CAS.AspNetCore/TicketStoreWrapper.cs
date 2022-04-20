using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class TicketStoreWrapper : ITicketStore
    {
        private readonly IServiceTicketStore _store;

        public TicketStoreWrapper(
            IServiceTicketStore store)
        {
            _store = store;
        }

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var serviceTicket = BuildServiceTicket(ticket);
            return await _store.StoreAsync(serviceTicket).ConfigureAwait(false);
        }

        public async Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            var ticket = await _store.RetrieveAsync(key).ConfigureAwait(false);
            return ticket == null ? null : BuildAuthenticationTicket(ticket);
        }

        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            var serviceTicket = BuildServiceTicket(ticket);
            await _store.RenewAsync(key, serviceTicket).ConfigureAwait(false);
        }

        public async Task RemoveAsync(string key)
        {
            await _store.RemoveAsync(key).ConfigureAwait(false);
        }

        private static ServiceTicket BuildServiceTicket(AuthenticationTicket ticket)
        {
            return new ServiceTicket(
                ticket.Properties.GetServiceTicket() ?? Guid.NewGuid().ToString(),
                ticket.Principal.Claims,
                string.IsNullOrWhiteSpace(ticket.Principal.Identity.AuthenticationType)
                    ? ticket.AuthenticationScheme
                    : ticket.Principal.Identity.AuthenticationType,
                ticket.Properties.IssuedUtc,
                ticket.Properties.ExpiresUtc);
        }

        private static AuthenticationTicket BuildAuthenticationTicket(ServiceTicket ticket)
        {
            return new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(ticket.Claims, ticket.AuthenticationType)),
                new AuthenticationProperties { IssuedUtc = ticket.IssuedUtc, ExpiresUtc = ticket.ExpiresUtc },
                ticket.AuthenticationType);
        }
    }
}