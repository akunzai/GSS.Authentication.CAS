using System;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;

namespace GSS.Authentication.CAS.AspNetCore;

public class DistributedCacheTicketStoreOptions
{
    public Func<string, string> CacheKeyFactory { get; set; } = key => $"CAS-Ticket:{key}";

    public Func<AuthenticationTicket, string> TicketIdFactory { get; set; } = ticket =>
    {
        var serviceTicket = ticket.Properties.GetServiceTicket();
        return !string.IsNullOrEmpty(serviceTicket) ? serviceTicket : Guid.NewGuid().ToString();
    };

    public JsonSerializerOptions? SerializerOptions { get; set; }

    public DistributedCacheEntryOptions CacheEntryOptions { get; set; } = new DistributedCacheEntryOptions();
}