using System;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS
{
    public class DistributedCacheServiceTicketStore : IServiceTicketStore
    {
        protected const string Prefix = "cas-st";
        protected IDistributedCache cache;

        public DistributedCacheServiceTicketStore()
        {
            cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
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
            var value = Serialize(ticket);
            await cache.SetAsync(
                CombineKey(ticket.TicketId), 
                value, 
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = ticket.Assertion.ValidUntil
                }).ConfigureAwait(false);
            return ticket.TicketId;
        }

        public async Task<ServiceTicket> RetrieveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            var value = await cache.GetAsync(CombineKey(key)).ConfigureAwait(false);
            return Deserialize<ServiceTicket>(value);
        }

        public Task RenewAsync(string key, ServiceTicket ticket)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            var value = Serialize(ticket);
            return cache.RemoveAsync(CombineKey(key))
                .ContinueWith(x =>
               cache.SetAsync(CombineKey(key), value, new DistributedCacheEntryOptions()));
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return cache.RemoveAsync(CombineKey(key));
        }

        protected virtual string CombineKey(string key)
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
