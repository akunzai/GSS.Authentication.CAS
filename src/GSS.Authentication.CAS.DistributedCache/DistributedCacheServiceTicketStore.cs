using System;
using System.Text.Json;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS
{
    public class DistributedCacheServiceTicketStore : IServiceTicketStore
    {
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheServiceTicketStoreOptions _options;

        public DistributedCacheServiceTicketStore(IDistributedCache cache,
            IOptions<DistributedCacheServiceTicketStoreOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }

        [Obsolete("Use the constructor that takes an IOptions<DistributedCacheServiceTicketStoreOptions> instead.")]
        public DistributedCacheServiceTicketStore(IDistributedCache cache) : this(cache,
            Options.Create(DistributedCacheServiceTicketStoreOptions.Default))
        {
        }

        [Obsolete("Use DistributedCacheServiceTicketStoreOptions instead.")]
        public static Func<string, string> CacheKeyFactory { get; set; } =
            key => $"{DistributedCacheServiceTicketStoreOptions.Prefix}:{key}";

        [Obsolete("Use DistributedCacheServiceTicketStoreOptions instead.")]
        public static JsonSerializerOptions? SerializerOptions { get; set; }

        public async Task<string> StoreAsync(ServiceTicket ticket)
        {
            var holder = new ServiceTicketHolder(ticket);
            var key = GetCacheKey(ticket.TicketId);
            var value = Serialize(holder);
            var cacheOptions = CloneCacheOptions(ticket.ExpiresUtc);
            await _cache.SetAsync(
                key,
                value,
                cacheOptions).ConfigureAwait(false);
            return ticket.TicketId;
        }

        public async Task<ServiceTicket?> RetrieveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            var cacheKey = GetCacheKey(key);
            var value = await _cache.GetAsync(cacheKey).ConfigureAwait(false);
            if (value == null || value.Length == 0)
                return null;
            return (ServiceTicket)Deserialize<ServiceTicketHolder>(value);
        }

        public async Task RenewAsync(string key, ServiceTicket ticket)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            var holder = new ServiceTicketHolder(ticket);
            var value = Serialize(holder);
            var cacheKey = GetCacheKey(key);
            await _cache.RemoveAsync(cacheKey).ConfigureAwait(false);
            var cacheOptions = CloneCacheOptions(ticket.ExpiresUtc);
            await _cache.SetAsync(cacheKey, value, cacheOptions).ConfigureAwait(false);
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            return _cache.RemoveAsync(GetCacheKey(key));
        }

        private DistributedCacheEntryOptions CloneCacheOptions(DateTimeOffset? expiresUtc)
        {
            return new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiresUtc ?? _options.CacheEntryOptions.AbsoluteExpiration,
                AbsoluteExpirationRelativeToNow = _options.CacheEntryOptions.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = _options.CacheEntryOptions.SlidingExpiration,
            };
        }

        private string GetCacheKey(string key)
        {
            var cacheKeyFromOptions = _options.CacheKeyFactory(key);
#pragma warning disable CS0618
            var cacheKeyFromProperty = CacheKeyFactory(key);
#pragma warning restore CS0618
            return string.Equals(cacheKeyFromOptions, cacheKeyFromProperty, StringComparison.Ordinal)
                ? cacheKeyFromOptions
                : cacheKeyFromProperty;
        }

        private byte[] Serialize(object value)
        {
#pragma warning disable CS0618
            return JsonSerializer.SerializeToUtf8Bytes(value, _options.SerializerOptions ?? SerializerOptions);
#pragma warning restore CS0618
        }

        private T Deserialize<T>(byte[] value)
        {
            var readOnlySpan = new ReadOnlySpan<byte>(value);
#pragma warning disable CS0618
            return JsonSerializer.Deserialize<T>(readOnlySpan, _options.SerializerOptions ?? SerializerOptions);
#pragma warning restore CS0618
        }
    }
}