using System;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace GSS.Authentication.CAS
{
    public class DistributedCacheServiceTicketStore : IServiceTicketStore
    {
        protected const string Prefix = "cas-st";
        protected IDistributedCache cache;

        public DistributedCacheServiceTicketStore()
        {
            cache = new MemoryDistributedCache(new MemoryCache(new MemoryCacheOptions()));
        }

        public DistributedCacheServiceTicketStore(IDistributedCache cacheClient)
        {
            this.cache = cacheClient;
        }

        JsonSerializerSettings SerializerSettings { get; set; } = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public async Task<string> StoreAsync(ServiceTicket ticket)
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpiration = ticket.Assertion.ValidUntil };
            var value = Serialize(ticket);
            await cache.SetAsync(CombindKey(ticket.TicketId), value).ConfigureAwait(false);
            return ticket.TicketId;
        }

        public async Task<ServiceTicket> RetrieveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            var value = await cache.GetAsync(CombindKey(key)).ConfigureAwait(false);
            return Deserialize<ServiceTicket>(value);
        }

        public Task RenewAsync(string key, ServiceTicket ticket)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            var value = Serialize(ticket);
            return cache.RemoveAsync(CombindKey(key))
                .ContinueWith(x =>
               cache.SetAsync(CombindKey(key), value, new DistributedCacheEntryOptions()));
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return cache.RemoveAsync(CombindKey(key));
        }

        protected virtual string CombindKey(string key)
        {
            return $"{Prefix}:{key}";
        }

        protected virtual byte[] Serialize(object value)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, SerializerSettings));
        }

        protected virtual T Deserialize<T>(byte[] value)
        {
            if (value == null) return default(T);
            var json = Encoding.UTF8.GetString(value, 0, value.Length);
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings);
        }
    }
}
