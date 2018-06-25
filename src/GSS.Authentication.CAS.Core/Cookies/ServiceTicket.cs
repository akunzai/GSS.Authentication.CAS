using System;
using System.Collections.Generic;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS
{
    public class ServiceTicket
    {
        public ServiceTicket(string ticketId, Assertion assertion, IEnumerable<ClaimWrapper> claims, string authenticationType)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) throw new ArgumentNullException(nameof(ticketId));
            if (string.IsNullOrWhiteSpace(authenticationType)) throw new ArgumentNullException(nameof(authenticationType));
            TicketId = ticketId;
            Assertion = assertion ?? throw new ArgumentNullException(nameof(assertion));
            Claims = claims;
            AuthenticationType = authenticationType;
        }

        public string TicketId { get; }

        public string AuthenticationType { get; }

        public Assertion Assertion { get; }

        public IEnumerable<ClaimWrapper> Claims { get; }
    }
}
