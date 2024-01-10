using System;
using System.Collections.Generic;
using System.Security.Claims;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS
{
    public class ServiceTicket
    {
        [Obsolete("Use another constructor instead.")]
        public ServiceTicket(string ticketId,
            Assertion? assertion,
            IEnumerable<Claim> claims,
            string authenticationType,
            DateTimeOffset? issuedUtc = null,
            DateTimeOffset? expiresUtc = null)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentNullException(nameof(ticketId));
            if (string.IsNullOrWhiteSpace(authenticationType))
                throw new ArgumentNullException(nameof(authenticationType));
            TicketId = ticketId;
#pragma warning disable CS0618
            Assertion = assertion;
#pragma warning restore CS0618
            Claims = claims;
            AuthenticationType = authenticationType;
            IssuedUtc = issuedUtc;
            ExpiresUtc = expiresUtc;
        }

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

        [Obsolete("Use Claims instead. Will be removed in future release.")]
        public Assertion? Assertion { get; }

        [Obsolete("Use IssuedUtc instead. Will be removed in future release.")]
        public DateTimeOffset? ValidFrom => IssuedUtc;

        [Obsolete("Use ExpiresUtc instead. Will be removed in future release.")]
        public DateTimeOffset? ValidUntil => ExpiresUtc;
    }
}