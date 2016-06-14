using System;

namespace GSS.Authentication.CAS
{
    public class CasOptions : ICasOptions
    {
        public string AuthenticationType { get; set; }

        public string CasServerUrlBase { get; set; }
    }
}
