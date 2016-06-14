using Microsoft.AspNetCore.Http.Authentication;

namespace GSS.Authentication.CAS.AspNetCore
{
    public static class TicketStoreExtensions
    {
        private const string ServiceTicketKey = "ServiceTicket";

        public static void SetServiceTicket(this AuthenticationProperties properties, string ticket)
        {
            properties.Items.Add(ServiceTicketKey, ticket);
        }

        public static string GetServiceTicket(this AuthenticationProperties properties)
        {
            string ticket = null;
            properties.Items.TryGetValue(ServiceTicketKey, out ticket);
            return (string.IsNullOrWhiteSpace(ticket)) ? null : ticket;
        }
    }
}
