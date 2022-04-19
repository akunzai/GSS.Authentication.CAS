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
            DateTimeOffset? validFrom = null,
            DateTimeOffset? validUntil = null)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) throw new ArgumentNullException(nameof(ticketId));
            if (string.IsNullOrWhiteSpace(authenticationType))
                throw new ArgumentNullException(nameof(authenticationType));
            TicketId = ticketId;
#pragma warning disable CS0618
            Assertion = assertion;
#pragma warning restore CS0618
            Claims = claims;
            AuthenticationType = authenticationType;
            ValidFrom = validFrom;
            ValidUntil = validUntil;
        }
        
        public ServiceTicket(string ticketId,
            IEnumerable<Claim> claims,
            string authenticationType,
            DateTimeOffset? validFrom = null,
            DateTimeOffset? validUntil = null)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) throw new ArgumentNullException(nameof(ticketId));
            if (string.IsNullOrWhiteSpace(authenticationType))
                throw new ArgumentNullException(nameof(authenticationType));
            TicketId = ticketId;
            Claims = claims;
            AuthenticationType = authenticationType;
            ValidFrom = validFrom;
            ValidUntil = validUntil;
        }

        public string TicketId { get; }

        public string AuthenticationType { get; }

        [Obsolete("Use Claims instead. Will be removed in future release.")]
        public Assertion? Assertion { get; }

        public IEnumerable<Claim> Claims { get; }

        public DateTimeOffset? ValidFrom { get; }

        public DateTimeOffset? ValidUntil { get; }  
    }
}