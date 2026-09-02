using System.Threading;
using System.Threading.Tasks;

namespace GSS.Authentication.CAS.Proxy
{
    /// <summary>
    /// Correlates the PGTIOU returned in a ticket-validation response with the real Proxy Granting Ticket
    /// delivered separately to the <c>pgtUrl</c> callback. See CAS Protocol Specification §2.5.4/§3.3/§3.4.
    /// </summary>
    public interface IProxyGrantingTicketStore
    {
        /// <summary>
        /// Records the real Proxy Granting Ticket delivered to the <c>pgtUrl</c> callback, keyed by the PGTIOU
        /// that will later be returned in the ticket-validation response.
        /// </summary>
        Task StoreAsync(string proxyGrantingTicketIou, string proxyGrantingTicket,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Looks up the real Proxy Granting Ticket for a PGTIOU, or <see langword="null"/> if none has been
        /// recorded (e.g. the callback hasn't arrived yet, or was never requested).
        /// </summary>
        Task<string?> GetAsync(string proxyGrantingTicketIou, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a previously stored Proxy Granting Ticket, once it's no longer needed.
        /// </summary>
        Task RemoveAsync(string proxyGrantingTicketIou, CancellationToken cancellationToken = default);
    }
}
