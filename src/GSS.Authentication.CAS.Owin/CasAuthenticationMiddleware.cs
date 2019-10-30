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
            if (Options.Provider == null)
            {
                Options.Provider = new CasAuthenticationProvider();
            }
            if (Options.StateDataFormat == null)
            {
                var dataProtecter = app.CreateDataProtector(
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
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable IDE0067 // Dispose objects before losing scope
                var httpClient = new HttpClient(Options.BackchannelHttpHandler ?? new HttpClientHandler())
#pragma warning restore IDE0067 // Dispose objects before losing scope
#pragma warning restore CA2000 // Dispose objects before losing scope
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
            return new CasAuthenticationHandler(_logger);
        }
    }
}
