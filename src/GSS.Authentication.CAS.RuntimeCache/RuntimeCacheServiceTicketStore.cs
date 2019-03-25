using System;
using System.Threading.Tasks;
using System.Runtime.Caching;

namespace GSS.Authentication.CAS
{
    [Obsolete("Use DistributedCacheServiceTicketStore instead")]
    public class RuntimeCacheServiceTicketStore : IServiceTicketStore
    {
        private const string Prefix = "cas-st";
        private readonly ObjectCache _cache;

        public RuntimeCacheServiceTicketStore() : this(MemoryCache.Default)
        {
        }

        public RuntimeCacheServiceTicketStore(ObjectCache cache)
        {
            _cache = cache;
        }

        public static Func<string,string> CacheKeyFactory { get; set; } = (key) => $"{Prefix}:{key}";

        public Task<string> StoreAsync(ServiceTicket ticket)
        {
            var policy = new CacheItemPolicy();
            if (ticket.Assertion.ValidUntil != null)
            {
                policy.AbsoluteExpiration = ticket.Assertion.ValidUntil.Value.ToLocalTime();
            }
            _cache.Add(CacheKeyFactory(ticket.TicketId), ticket, policy);
            return Task.FromResult(ticket.TicketId);
        }

        public Task<ServiceTicket> RetrieveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return Task.FromResult((ServiceTicket)_cache.Get(CacheKeyFactory(key)));
        }

        public Task RenewAsync(string key, ServiceTicket ticket)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            var policy = new CacheItemPolicy();
            if (ticket.Assertion.ValidUntil != null)
            {
                policy.AbsoluteExpiration = ticket.Assertion.ValidUntil.Value.ToLocalTime();
            }
            _cache.Set(CacheKeyFactory(ticket.TicketId), ticket, policy);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            _cache.Remove(CacheKeyFactory(key));
            return Task.CompletedTask;
        }
    }
}
