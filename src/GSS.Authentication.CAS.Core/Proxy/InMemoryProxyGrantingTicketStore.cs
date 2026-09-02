using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GSS.Authentication.CAS.Proxy
{
    /// <summary>
    /// Default, single-process <see cref="IProxyGrantingTicketStore"/>. Register a distributed implementation
    /// for multi-instance deployments, since the PGTIOU callback and the validation response that references it
    /// may land on different instances.
    /// </summary>
    public class InMemoryProxyGrantingTicketStore : IProxyGrantingTicketStore
    {
        private readonly ConcurrentDictionary<string, string> _tickets = new();

        /// <inheritdoc />
        public Task StoreAsync(string proxyGrantingTicketIou, string proxyGrantingTicket,
            CancellationToken cancellationToken = default)
        {
            _tickets[proxyGrantingTicketIou] = proxyGrantingTicket;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<string?> GetAsync(string proxyGrantingTicketIou, CancellationToken cancellationToken = default)
        {
            _tickets.TryGetValue(proxyGrantingTicketIou, out var ticket);
            return Task.FromResult<string?>(ticket);
        }

        /// <inheritdoc />
        public Task RemoveAsync(string proxyGrantingTicketIou, CancellationToken cancellationToken = default)
        {
            _tickets.TryRemove(proxyGrantingTicketIou, out _);
            return Task.CompletedTask;
        }
    }
}
