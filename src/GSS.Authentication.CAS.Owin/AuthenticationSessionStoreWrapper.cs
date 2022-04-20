using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace GSS.Authentication.CAS.Owin
{
    public class AuthenticationSessionStoreWrapper : IAuthenticationSessionStore
    {
        private readonly IServiceTicketStore _store;

        public AuthenticationSessionStoreWrapper(
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
            return new ServiceTicket(ticket.Properties?.GetServiceTicket() ?? Guid.NewGuid().ToString(),
                ticket.Identity.Claims,
                ticket.Identity.AuthenticationType,
                ticket.Properties?.IssuedUtc,
                ticket.Properties?.ExpiresUtc);
        }

        private static AuthenticationTicket BuildAuthenticationTicket(ServiceTicket ticket)
        {
            return new AuthenticationTicket(
                new ClaimsIdentity(ticket.Claims, ticket.AuthenticationType),
                new AuthenticationProperties { IssuedUtc = ticket.IssuedUtc, ExpiresUtc = ticket.ExpiresUtc });
        }
    }
}