using Microsoft.Owin.Security;

namespace GSS.Authentication.CAS.Owin
{
    public static class AuthenticationSessionStoreExtensions
    {
        private const string ServiceTicketKey = "ServiceTicket";

        public static void SetServiceTicket(this AuthenticationProperties properties, string ticket)
        {
            properties.Dictionary.Add(ServiceTicketKey, ticket);
        }

        public static string GetServiceTicket(this AuthenticationProperties properties)
        {
            string ticket = null;
            properties.Dictionary.TryGetValue(ServiceTicketKey, out ticket);
            return (string.IsNullOrWhiteSpace(ticket)) ? null : ticket;
        }
    }
}
