namespace GSS.Authentication.CAS
{
    public static class Constants
    {
        public static class Parameters
        {
            public const string Service = "service";

            public const string Ticket = "ticket";

            public const string ProxyGrantingTicketId = "pgtId";

            public const string ProxyGrantingTicketIou = "pgtIou";
        }

        public static class Paths
        {
            public const string Login = "/login";

            public const string Logout = "/logout";
        }
    }
}