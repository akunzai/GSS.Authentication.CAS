using System;
using System.Threading.Tasks;
using System.Runtime.Caching;

namespace GSS.Authentication.CAS
{
    public class RuntimeCacheServiceTicketStore : IServiceTicketStore
    {
        protected const string Prefix = "cas-st";
        protected ObjectCache cache;

        public RuntimeCacheServiceTicketStore() : this(MemoryCache.Default)
        {
        }

        public RuntimeCacheServiceTicketStore(ObjectCache cache)
        {
            this.cache = cache;
        }

        public Task<string> StoreAsync(ServiceTicket ticket)
        {
            var policy = new CacheItemPolicy();
            if (ticket.Assertion.ValidUntil != null)
            {
                policy.AbsoluteExpiration = ticket.Assertion.ValidUntil.Value.ToLocalTime();
            }
            cache.Add(CombineKey(ticket.TicketId), ticket, policy);
            return Task.FromResult(ticket.TicketId);
        }

        public Task<ServiceTicket> RetrieveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return Task.FromResult((ServiceTicket)cache.Get(CombineKey(key)));
        }

        public Task RenewAsync(string key, ServiceTicket ticket)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            var policy = new CacheItemPolicy();
            if (ticket.Assertion.ValidUntil != null)
            {
                policy.AbsoluteExpiration = ticket.Assertion.ValidUntil.Value.ToLocalTime();
            }
            cache.Set(CombineKey(ticket.TicketId), ticket, policy);
            return Task.FromResult(0);
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            cache.Remove(CombineKey(key));
            return Task.FromResult(0);
        }

        protected virtual string CombineKey(string key)
        {
            return $"{Prefix}:{key}";
        }
    }
}
