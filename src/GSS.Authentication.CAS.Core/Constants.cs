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

            public const string Renew = "renew";

            public const string Gateway = "gateway";

            public const string Method = "method";

            public const string Locale = "locale";
        }

        public static class Paths
        {
            public const string Login = "/login";

            public const string Logout = "/logout";
        }
    }
}