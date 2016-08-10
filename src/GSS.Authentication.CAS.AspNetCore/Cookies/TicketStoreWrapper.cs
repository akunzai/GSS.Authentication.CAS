using System;
using System.Linq;
using System.Security.Principal;
using System.Security.Claims;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Authentication;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class TicketStoreWrapper : ITicketStore
    {
        protected IServiceTicketStore store;

        public TicketStoreWrapper(
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
            var principal = ticket.Principal;
            var properties = ticket.Properties;
            var assertion = (principal as CasPrincipal)?.Assertion 
                ?? (principal.Identity as CasIdentity)?.Assertion 
                ?? new Assertion(
                    principal.GetPrincipalName(), 
                    null, properties.IssuedUtc, 
                    properties.ExpiresUtc);
            return new ServiceTicket(ticketId, assertion, principal.Claims.Select(x=>new ClaimWrapper(x)), principal.Identity.AuthenticationType);
        }

        protected AuthenticationTicket BuildAuthenticationTicket(ServiceTicket ticket)
        {
            if (ticket == null) return null;
            var assertion = ticket.Assertion;
            var principal = new CasPrincipal(assertion, ticket.AuthenticationType);
            var identity = (principal.Identity as ClaimsIdentity);
            if (identity != null)
            {
                foreach(var claim in ticket.Claims)
                {
                    if (identity.HasClaim(claim.Type, claim.Value)) continue;
                    identity.AddClaim(claim.ToClaim());
                }
            }
            return new AuthenticationTicket(
                principal, 
                new AuthenticationProperties {
                    IssuedUtc = assertion.ValidFrom,
                    ExpiresUtc = assertion.ValidUntil },
                ticket.AuthenticationType);
        }
    }
}
