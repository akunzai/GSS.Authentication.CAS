using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace GSS.Authentication.CAS.Owin
{
    public class AuthenticationSessionStoreWrapper : IAuthenticationSessionStore
    {
        protected IServiceTicketStore store;

        public AuthenticationSessionStoreWrapper(
            IServiceTicketStore store)
        {
            this.store = store;
        }

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var serviceTicket = BuildServiceTicket(ticket);
            return store.StoreAsync(serviceTicket);
        }

        public async Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            var ticket = await store.RetrieveAsync(key);
            return BuildAuthenticationTicket(ticket);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            var serviceTicket = BuildServiceTicket(ticket);
            return store.RenewAsync(key, serviceTicket);
        }

        public Task RemoveAsync(string key)
        {
            return store.RemoveAsync(key);
        }

        protected ServiceTicket BuildServiceTicket(AuthenticationTicket ticket)
        {
            var ticketId = ticket.Properties.GetServiceTicket() ?? Guid.NewGuid().ToString();
            var identity = ticket.Identity;
            var properties = ticket.Properties;
            var assertion = (identity as CasIdentity)?.Assertion 
                ?? new Assertion(
                    identity.GetPrincipalName(), 
                    null, 
                    properties.IssuedUtc, 
                    properties.ExpiresUtc);
            return new ServiceTicket(ticketId, assertion, identity.Claims.ToDictionary(), identity.AuthenticationType);
        }

        protected AuthenticationTicket BuildAuthenticationTicket(ServiceTicket ticket)
        {
            if (ticket == null) return null;
            var assertion = ticket.Assertion;
            var identity = new CasIdentity(assertion, ticket.AuthenticationType);
            identity.AddClaims(ticket.Claims);
            return new AuthenticationTicket(
                identity, 
                new AuthenticationProperties {
                    IssuedUtc = assertion.ValidFrom,
                    ExpiresUtc = assertion.ValidUntil });
        }
    }
}
