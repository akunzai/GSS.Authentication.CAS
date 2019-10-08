using System;
using System.Linq;
using System.Security.Principal;
using System.Security.Claims;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
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

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var serviceTicket = BuildServiceTicket(ticket);
            return _store.StoreAsync(serviceTicket);
        }

        public async Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            var ticket = await _store.RetrieveAsync(key).ConfigureAwait(false);
            return BuildAuthenticationTicket(ticket);
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

        protected ServiceTicket BuildServiceTicket(AuthenticationTicket ticket)
        {
            var ticketId = ticket?.Properties.GetTokenValue("access_token") ?? Guid.NewGuid().ToString();
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
            if (!(principal.Identity is ClaimsIdentity identity))
            {
                return new AuthenticationTicket(
                   principal,
                   new AuthenticationProperties
                   {
                       IssuedUtc = assertion.ValidFrom,
                       ExpiresUtc = assertion.ValidUntil
                   },
                   ticket.AuthenticationType);
            }

            foreach (var claim in ticket.Claims)
            {
                if (identity.HasClaim(claim.Type, claim.Value)) continue;
                identity.AddClaim(claim.ToClaim());
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
