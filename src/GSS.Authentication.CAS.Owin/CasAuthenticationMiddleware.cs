using System;
using System.Net.Http;
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
    public class CasAuthenticationMiddleware : AuthenticationMiddleware<CasAuthenticationOptions>
    {
        private readonly ILogger logger;
        public CasAuthenticationMiddleware(
            OwinMiddleware next,
            IAppBuilder app,
            CasAuthenticationOptions options) : base(next, options)
        {
            if (!Options.CallbackPath.HasValue) throw new ArgumentNullException(nameof(Options.CallbackPath));
            if (string.IsNullOrEmpty(Options.CasServerUrlBase)) throw new ArgumentNullException(nameof(Options.CasServerUrlBase));

            logger = app.CreateLogger<CasAuthenticationMiddleware>();
            if (Options.Provider == null)
            {
                Options.Provider = new CasAuthenticationProvider();
            }
            if (Options.StateDataFormat == null)
            {
                IDataProtector dataProtecter = app.CreateDataProtector(
                    typeof(CasAuthenticationMiddleware).FullName,
                    Options.AuthenticationType, "v1");
                Options.StateDataFormat = new PropertiesDataFormat(dataProtecter);
            }
            if (string.IsNullOrEmpty(Options.SignInAsAuthenticationType))
            {
                Options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }

            if (Options.ServiceTicketValidator == null)
            {
                var httpClient = new HttpClient(Options.BackchannelHttpHandler ?? new HttpClientHandler())
                {
                    Timeout = Options.BackchannelTimeout,
                    MaxResponseContentBufferSize = 1024 * 1024 * 10 // 10 MB
                };
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ASP.NET CAS middleware");
                httpClient.DefaultRequestHeaders.ExpectContinue = false;
                Options.ServiceTicketValidator = new Cas30ServiceTicketValidator(Options, httpClient);
            }
        }

        protected override AuthenticationHandler<CasAuthenticationOptions> CreateHandler()
        {
            return new CasAuthenticationHandler(logger);
        }
    }
}
