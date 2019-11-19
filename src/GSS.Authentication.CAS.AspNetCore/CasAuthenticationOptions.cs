using System;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;

namespace GSS.Authentication.CAS
{
    /// <summary>
    /// Configuration options for <see cref="CasAuthenticationHandler{TOptions}"/>
    /// </summary>
    public class CasAuthenticationOptions : RemoteAuthenticationOptions, ICasOptions
    {
        public CasAuthenticationOptions()
        {
            CallbackPath = "/signin-cas";
            Events = new CasEvents();
        }

        public string CasServerUrlBase { get; set; } = default!;

        public string AuthenticationType => CasDefaults.AuthenticationType;

        public IServiceTicketValidator ServiceTicketValidator { get; set; } = default!;

        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; } = default!;

        public new CasEvents Events
        {
            get => (CasEvents)base.Events;
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
