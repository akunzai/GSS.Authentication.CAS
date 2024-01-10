using System;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace GSS.Authentication.CAS
{
    public class DistributedCacheServiceTicketStoreOptions
    {
        public static readonly DistributedCacheServiceTicketStoreOptions Default = new();

        internal const string Prefix = "CAS-ST";

        public Func<string, string> CacheKeyFactory { get; set; } = key => $"{Prefix}:{key}";

        public JsonSerializerOptions? SerializerOptions { get; set; }

        public DistributedCacheEntryOptions CacheEntryOptions { get; set; } = new DistributedCacheEntryOptions();
    }
}