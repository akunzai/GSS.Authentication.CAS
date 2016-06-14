using System;
using System.Net.Http;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.Owin;
using GSS.Authentication.CAS.Validation;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace Owin
{
    public class CasAuthenticationOptions : AuthenticationOptions, ICasOptions
    {
        public const string Scheme = "CAS";
        public CasAuthenticationOptions() : base(Scheme)
        {
            Caption = Scheme;
            CallbackPath = new PathString("/signin-cas");
            AuthenticationMode = AuthenticationMode.Passive;
            BackchannelTimeout = TimeSpan.FromSeconds(60);
        }

        public TimeSpan BackchannelTimeout { get; set; }

        public HttpMessageHandler BackchannelHttpHandler { get; set; }

        public string Caption
        {
            get { return Description.Caption; }
            set { Description.Caption = value; }
        }

        public PathString CallbackPath { get; set; }

        public string SignInAsAuthenticationType { get; set; }

        public bool UseAuthenticationSessionStore { get; set; }

        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

        #region ICasOptions
        public string CasServerUrlBase { get; set; }
        #endregion

        public IServiceTicketValidator ServiceTicketValidator { get; set; }

        public ICasAuthenticationProvider Provider { get; set; }
    }
}
