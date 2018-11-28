using System;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace GSS.Authentication.CAS
{
    public class DistributedCacheServiceTicketStore : IServiceTicketStore
    {
        private const string Prefix = "cas-st";
        private readonly IDistributedCache _cache;

        public DistributedCacheServiceTicketStore(IDistributedCache cache)
        {
            _cache = cache;
        }

        public static Func<string, string> CacheKeyFactory { get; set; } = (key) => $"{Prefix}:{key}";

        public static JsonSerializerSettings SerializerSettings { get; set; } = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public async Task<string> StoreAsync(ServiceTicket ticket)
        {
            var value = Serialize(ticket);
            await _cache.SetAsync(
                CacheKeyFactory(ticket.TicketId),
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
            var value = await _cache.GetAsync(CacheKeyFactory(key)).ConfigureAwait(false);
            return Deserialize<ServiceTicket>(value);
        }

        public async Task RenewAsync(string key, ServiceTicket ticket)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            var value = Serialize(ticket);
            await _cache.RemoveAsync(CacheKeyFactory(key)).ConfigureAwait(false);

            await _cache.SetAsync(CacheKeyFactory(key), value, new DistributedCacheEntryOptions
               {
                   AbsoluteExpiration = ticket.Assertion.ValidUntil
               }).ConfigureAwait(false);
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return _cache.RemoveAsync(CacheKeyFactory(key));
        }

        private static byte[] Serialize(object value)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, SerializerSettings));
        }

        private static T Deserialize<T>(byte[] value)
        {
            if (value == null) return default;
            var json = Encoding.UTF8.GetString(value, 0, value.Length);
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings);
        }
    }
}
