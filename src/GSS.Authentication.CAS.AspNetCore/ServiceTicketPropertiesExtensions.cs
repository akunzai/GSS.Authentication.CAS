using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;

namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Stores and reads the CAS service ticket on <see cref="AuthenticationProperties"/>, which is how
/// <see cref="DistributedCacheTicketStore"/> keys its entries so Single Sign-Out can find them.
/// </summary>
public static class ServiceTicketPropertiesExtensions
{
    private const string ServiceTicketKey = "service_ticket";

    public static void SetServiceTicket(this AuthenticationProperties properties, string ticket)
    {
        properties.StoreTokens(new List<AuthenticationToken> { new() { Name = ServiceTicketKey, Value = ticket } });
    }

    public static string? GetServiceTicket(this AuthenticationProperties properties)
    {
        var ticket = properties.GetTokenValue(ServiceTicketKey);
        return string.IsNullOrWhiteSpace(ticket) ? null : ticket;
    }
}