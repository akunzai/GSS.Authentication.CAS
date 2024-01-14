using System;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Owin.Security;

namespace GSS.Authentication.CAS.Owin
{
    public class DistributedCacheIAuthenticationSessionStoreOptions
    {
        public Func<string, string> CacheKeyFactory { get; set; } = key => $"CAS-Ticket:{key}";

        public Func<AuthenticationTicket, string> TicketIdFactory { get; set; } = ticket =>
        {
            var serviceTicket = ticket.Properties.GetServiceTicket();
            if (serviceTicket != null && !string.IsNullOrEmpty(serviceTicket))
            {
                return serviceTicket;
            }

            return Guid.NewGuid().ToString();
        };

        public JsonSerializerOptions? SerializerOptions { get; set; }

        public DistributedCacheEntryOptions CacheEntryOptions { get; set; } = new DistributedCacheEntryOptions();
    }
}