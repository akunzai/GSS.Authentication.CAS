using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace GSS.Authentication.CAS.Internal
{
    /// <summary>
    /// ServiceTicket Holder for Serialization
    /// </summary>
    internal struct ServiceTicketHolder
    {
        public ServiceTicketHolder(ServiceTicket ticket)
        {
            TicketId = ticket.TicketId;
            AuthenticationType = ticket.AuthenticationType;
            Claims = ticket.Claims.Select(x=> new ClaimHolder(x));
        }

        public string TicketId { get; set; }

        public string AuthenticationType { get; set; }

        public IEnumerable<ClaimHolder> Claims { get; set; }

        public static explicit operator ServiceTicket(ServiceTicketHolder h) =>
            new ServiceTicket(h.TicketId, h.Claims.Select(x => (Claim)x), h.AuthenticationType);
    }
}
