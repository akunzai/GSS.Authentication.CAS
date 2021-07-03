using System;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
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

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var serviceTicket = BuildServiceTicket(ticket);
            return _store.StoreAsync(serviceTicket);
        }

        public async Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            var ticket = await _store.RetrieveAsync(key).ConfigureAwait(false);
            return ticket == null ? null : BuildAuthenticationTicket(ticket);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            var serviceTicket = BuildServiceTicket(ticket);
            return _store.RenewAsync(key, serviceTicket);
        }

        public Task RemoveAsync(string key)
        {
            return _store.RemoveAsync(key);
        }

        private static ServiceTicket BuildServiceTicket(AuthenticationTicket ticket)
        {
            var identity = ticket.Identity;
            var properties = ticket.Properties;
            var ticketId = properties?.GetServiceTicket() ?? Guid.NewGuid().ToString();
            var assertion = (identity as CasIdentity)?.Assertion
                ?? new Assertion(
                    identity.GetPrincipalName(),
                    null,
                    properties?.IssuedUtc,
                    properties?.ExpiresUtc);
            return new ServiceTicket(ticketId, assertion, identity.Claims, identity.AuthenticationType);
        }

        private static AuthenticationTicket BuildAuthenticationTicket(ServiceTicket ticket)
        {
            var assertion = ticket.Assertion;
            var identity = new CasIdentity(assertion, ticket.AuthenticationType);
            identity.AddClaims(ticket.Claims);
            return new AuthenticationTicket(
                identity,
                new AuthenticationProperties
                {
                    IssuedUtc = assertion.ValidFrom,
                    ExpiresUtc = assertion.ValidUntil
                });
        }
    }
}
