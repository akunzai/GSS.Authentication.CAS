using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Owin.Security;

namespace GSS.Authentication.CAS.Owin.Internal
{
    internal struct AuthenticationTicketHolder
    {
        public AuthenticationTicketHolder(AuthenticationTicket ticket)
        {
            AuthenticationType = ticket.Identity.AuthenticationType;
            Claims = ticket.Identity.Claims.Select(x => new ClaimHolder(x));
            NameClaimType = ticket.Identity?.NameClaimType;
            RoleClaimType = ticket.Identity?.RoleClaimType;
            Properties = ticket.Properties.Dictionary;
        }

        public string AuthenticationType { get; set; }

        public IEnumerable<ClaimHolder> Claims { get; set; }

        public string? NameClaimType { get; set; }

        public string? RoleClaimType { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public static explicit operator AuthenticationTicket(AuthenticationTicketHolder h)
        {
            var identity = new ClaimsIdentity(h.Claims.Select(x => (Claim)x), h.AuthenticationType, h.NameClaimType,
                h.RoleClaimType);
            var properties = new AuthenticationProperties(h.Properties);
            return new AuthenticationTicket(identity, properties);
        }
    }
}