using System;
using GSS.Authentication.CAS.Validation;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace GSS.Authentication.CAS.Owin
{
    /// <summary>
    /// OWIN middleware for authenticating users using CAS protocol
    /// </summary>
    public class CasAuthenticationMiddleware : AuthenticationMiddleware<CasAuthenticationOptions>
    {
        private readonly ILogger _logger;

        public CasAuthenticationMiddleware(
            OwinMiddleware next,
            IAppBuilder app,
            CasAuthenticationOptions options) : base(next, options)
        {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            if (!Options.CallbackPath.HasValue) throw new ArgumentNullException(nameof(Options.CallbackPath));
            if (string.IsNullOrEmpty(Options.CasServerUrlBase)) throw new ArgumentNullException(nameof(Options.CasServerUrlBase));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

            _logger = app.CreateLogger<CasAuthenticationMiddleware>();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Options.Provider == null)
            {
                Options.Provider = new CasAuthenticationProvider();
            }

            if (Options.StateDataFormat == null)
            {
                var dataProtector = app.CreateDataProtector(
                    typeof(CasAuthenticationMiddleware).FullName,
                    Options.AuthenticationType, "v1");
                Options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }

            if (string.IsNullOrEmpty(Options.SignInAsAuthenticationType))
            {
                Options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Options.ServiceTicketValidator == null)
            {
                var httpClient = Options.BackchannelFactory(Options);
                Options.ServiceTicketValidator = new Cas30ServiceTicketValidator(Options, httpClient);
            }
        }

        protected override AuthenticationHandler<CasAuthenticationOptions> CreateHandler()
        {
            return new CasAuthenticationHandler(_logger);
        }
    }
}
