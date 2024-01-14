using System;
using System.Text.Json;
using System.Threading.Tasks;
using GSS.Authentication.CAS.AspNetCore.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

public class DistributedCacheTicketStore : ITicketStore
{
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheTicketStoreOptions _options;

    public DistributedCacheTicketStore(IDistributedCache cache, IOptions<DistributedCacheTicketStoreOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var id = _options.TicketIdFactory(ticket);
        var cacheKey = _options.CacheKeyFactory(id);
        var holder = new AuthenticationTicketHolder(ticket);
        var value = JsonSerializer.SerializeToUtf8Bytes(holder, _options.SerializerOptions);
        var cacheOptions = CloneCacheOptions(ticket.Properties.ExpiresUtc);
        await _cache.SetAsync(cacheKey, value, cacheOptions).ConfigureAwait(false);
        return id;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        var cacheKey = _options.CacheKeyFactory(key);
        await _cache.RemoveAsync(cacheKey).ConfigureAwait(false);
        var holder = new AuthenticationTicketHolder(ticket);
        var value = JsonSerializer.SerializeToUtf8Bytes(holder, _options.SerializerOptions);
        var cacheOptions = CloneCacheOptions(ticket.Properties.ExpiresUtc);
        await _cache.SetAsync(cacheKey, value, cacheOptions).ConfigureAwait(false);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        var cacheKey = _options.CacheKeyFactory(key);
        var value = await _cache.GetAsync(cacheKey).ConfigureAwait(false);
        if (value == null || value.Length == 0)
            return null;
        var holder =
            JsonSerializer.Deserialize<AuthenticationTicketHolder>(new ReadOnlySpan<byte>(value),
                _options.SerializerOptions);
        return (AuthenticationTicket)holder;
    }

    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        await _cache.RemoveAsync(_options.CacheKeyFactory(key));
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
}