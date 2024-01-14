using System;
using System.Security.Claims;
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
        if (!string.IsNullOrEmpty(serviceTicket))
        {
            return serviceTicket;
        }

        var claimsIdentity = ticket.Principal.Identity as ClaimsIdentity;
        var id = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return id ?? Guid.NewGuid().ToString();
    };

    public JsonSerializerOptions? SerializerOptions { get; set; }

    public DistributedCacheEntryOptions CacheEntryOptions { get; set; } = new DistributedCacheEntryOptions();
}