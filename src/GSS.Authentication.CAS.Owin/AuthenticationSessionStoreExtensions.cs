using Microsoft.Owin.Security;

namespace GSS.Authentication.CAS.Owin
{
    public static class AuthenticationSessionStoreExtensions
    {
        private const string ServiceTicketKey = "service_ticket";

        public static void SetServiceTicket(this AuthenticationProperties properties, string ticket)
        {
            properties.Dictionary.Add(ServiceTicketKey, ticket);
        }

        public static string? GetServiceTicket(this AuthenticationProperties properties)
        {
            properties.Dictionary.TryGetValue(ServiceTicketKey, out var ticket);
            return string.IsNullOrWhiteSpace(ticket) ? null : ticket;
        }
    }
}
