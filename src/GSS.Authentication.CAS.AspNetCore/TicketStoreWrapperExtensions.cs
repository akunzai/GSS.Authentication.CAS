using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;

namespace GSS.Authentication.CAS.AspNetCore
{
    public static class TicketStoreWrapperExtensions
    {
        private const string ServiceTicketKey = "access_token";

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
}