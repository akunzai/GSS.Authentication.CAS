using System;
using GSS.Authentication.CAS;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;

namespace Microsoft.AspNetCore.Builder 
{
    public class CasAuthenticationOptions : RemoteAuthenticationOptions, ICasOptions
    {
        public const string Scheme = "CAS";
        public CasAuthenticationOptions()
        {
            AuthenticationScheme = Scheme;
            DisplayName = AuthenticationScheme;
            CallbackPath = new PathString("/signin-cas");
            BackchannelTimeout = TimeSpan.FromSeconds(60);
            Events = new CasEvents();
        }

        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

        #region ICasOptions
        public string CasServerUrlBase { get; set; }
        public string AuthenticationType { get { return AuthenticationScheme; } }
        #endregion

        public bool UseTicketStore { get; set; }

        public IServiceTicketValidator ServiceTicketValidator { get; set; }

        public new ICasEvents Events
        {
            get { return base.Events as ICasEvents; }
            set { base.Events = value; }
        }
    }
}
