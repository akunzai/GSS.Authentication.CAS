using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace GSS.Authentication.CAS
{
    [Obsolete("Will be removed in future release.")]
    public class ServiceTicket
    {
        public ServiceTicket(string ticketId,
            IEnumerable<Claim> claims,
            string authenticationType,
            DateTimeOffset? issuedUtc = null,
            DateTimeOffset? expiresUtc = null,
            string? nameClaimType = null,
            string? roleClaimType = null)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentNullException(nameof(ticketId));
            if (string.IsNullOrWhiteSpace(authenticationType))
                throw new ArgumentNullException(nameof(authenticationType));
            TicketId = ticketId;
            Claims = claims;
            AuthenticationType = authenticationType;
            IssuedUtc = issuedUtc;
            ExpiresUtc = expiresUtc;
            NameClaimType = nameClaimType;
            RoleClaimType = roleClaimType;
        }

        public string TicketId { get; }

        public string AuthenticationType { get; }

        public IEnumerable<Claim> Claims { get; }

        public string? NameClaimType { get; }

        public string? RoleClaimType { get; }

        public DateTimeOffset? IssuedUtc { get; }

        public DateTimeOffset? ExpiresUtc { get; }
    }
}