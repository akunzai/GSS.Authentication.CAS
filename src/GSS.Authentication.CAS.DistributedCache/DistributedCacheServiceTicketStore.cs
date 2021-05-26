using System;
using System.Text.Json;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Internal;
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

        public static JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions();

        public async Task<string> StoreAsync(ServiceTicket ticket)
        {
            var holder = new ServiceTicketHolder(ticket);
            var value = Serialize(holder);
            await _cache.SetAsync(
                CacheKeyFactory(ticket.TicketId),
                value,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = ticket.Assertion.ValidUntil
                }).ConfigureAwait(false);
            return ticket.TicketId;
        }

        public async Task<ServiceTicket?> RetrieveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            var value = await _cache.GetAsync(CacheKeyFactory(key)).ConfigureAwait(false);
            if (value == null || value.Length == 0)
                return null;
            return (ServiceTicket) Deserialize<ServiceTicketHolder>(value);
        }

        public async Task RenewAsync(string key, ServiceTicket ticket)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            var holder = new ServiceTicketHolder(ticket);
            var value = Serialize(holder);
            await _cache.RemoveAsync(CacheKeyFactory(key)).ConfigureAwait(false);

            await _cache.SetAsync(CacheKeyFactory(key), value, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = ticket.Assertion.ValidUntil
            }).ConfigureAwait(false);
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            return _cache.RemoveAsync(CacheKeyFactory(key));
        }

        private static byte[] Serialize(object value)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        }

        private static T? Deserialize<T>(byte[] value)
        {
            var readOnlySpan = new ReadOnlySpan<byte>(value);
            return JsonSerializer.Deserialize<T>(readOnlySpan, SerializerOptions);
        }
    }
}
