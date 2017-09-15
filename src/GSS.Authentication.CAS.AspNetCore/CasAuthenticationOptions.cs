using System;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;

namespace GSS.Authentication.CAS
{
    public class CasAuthenticationOptions : RemoteAuthenticationOptions, ICasOptions
    {
        public CasAuthenticationOptions()
        {
            CallbackPath = "/signin-cas";
            Events = new CasEvents();
        }

        #region ICasOptions

        public string CasServerUrlBase { get; set; }

        public string AuthenticationType => CasDefaults.AuthenticationType;
        
        #endregion

        public IServiceTicketValidator ServiceTicketValidator { get; set; }

        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

        public new CasEvents Events
        {
            get => base.Events as CasEvents;
            set => base.Events = value;
        }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(CasServerUrlBase))
            {
                throw new ArgumentException($"The '{nameof(CasServerUrlBase)}' option must be provided.");
            }
        }
    }
}
