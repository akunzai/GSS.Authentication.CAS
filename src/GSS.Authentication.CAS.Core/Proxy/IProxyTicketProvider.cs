using System.Threading;
using System.Threading.Tasks;

namespace GSS.Authentication.CAS.Proxy
{
    /// <summary>
    /// Exchanges a Proxy Granting Ticket for a Proxy Ticket via the CAS <c>/proxy</c> endpoint.
    /// See CAS Protocol Specification §2.7.
    /// </summary>
    public interface IProxyTicketProvider
    {
        /// <summary>
        /// Requests a Proxy Ticket for <paramref name="targetService"/>, using a previously obtained
        /// Proxy Granting Ticket.
        /// </summary>
        /// <param name="proxyGrantingTicket">The real PGT, e.g. from <see cref="IProxyGrantingTicketStore"/>.</param>
        /// <param name="targetService">The backend service the Proxy Ticket will be presented to.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The issued Proxy Ticket.</returns>
        /// <exception cref="System.Security.Authentication.AuthenticationException">
        /// Thrown when CAS returns a <c>cas:proxyFailure</c> response.
        /// </exception>
        Task<string> GetProxyTicketAsync(string proxyGrantingTicket, string targetService,
            CancellationToken cancellationToken = default);
    }
}
