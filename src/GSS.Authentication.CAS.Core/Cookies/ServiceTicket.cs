using System;
using System.Collections.Generic;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS
{
    public class ServiceTicket
    {
        public ServiceTicket(string ticketId, Assertion assertion, IDictionary<string, string> claims, string authenticationType)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) throw new ArgumentNullException(nameof(ticketId));
            if (string.IsNullOrWhiteSpace(authenticationType)) throw new ArgumentNullException(nameof(authenticationType));
            if (assertion == null) throw new ArgumentNullException(nameof(assertion));
            TicketId = ticketId;
            Assertion = assertion;
            Claims = claims;
            AuthenticationType = authenticationType;
        }

        public string TicketId { get; protected set; }

        public string AuthenticationType { get; protected set; }

        public Assertion Assertion { get; set; }

        public IDictionary<string,string> Claims { get; set; }
    }
}
