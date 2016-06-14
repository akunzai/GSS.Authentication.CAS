using System;
using System.Net.Http;
using System.Text.Encodings.Web;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class CasAuthenticationMiddleware : AuthenticationMiddleware<CasAuthenticationOptions>
    {
        public CasAuthenticationMiddleware(
            RequestDelegate next, 
            IDataProtectionProvider dataProtectionProvider,
            ILoggerFactory loggerFactory, 
            UrlEncoder encoder,
            IOptions<SharedAuthenticationOptions> sharedOptions,
            IOptions<CasAuthenticationOptions> options) : base(next, options, loggerFactory, encoder)
        {
            if (!Options.CallbackPath.HasValue) throw new ArgumentNullException(nameof(Options.CallbackPath));
            if (string.IsNullOrEmpty(Options.CasServerUrlBase)) throw new ArgumentNullException(nameof(Options.CasServerUrlBase));

            if (Options.Events == null)
            {
                Options.Events = new CasEvents();
            }
            if (Options.StateDataFormat == null)
            {
                var dataProtector = dataProtectionProvider.CreateProtector(
                    typeof(CasAuthenticationMiddleware).FullName, Options.AuthenticationScheme, "v1");
                Options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }

            if (string.IsNullOrEmpty(Options.SignInScheme))
            {
                Options.SignInScheme = sharedOptions.Value.SignInScheme;
            }

            if (Options.ServiceTicketValidator == null)
            {
                var httpClient = new HttpClient(Options.BackchannelHttpHandler ?? new HttpClientHandler());
                httpClient.Timeout = Options.BackchannelTimeout;
                httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ASP.NET CAS middleware");
                httpClient.DefaultRequestHeaders.ExpectContinue = false;
                Options.ServiceTicketValidator = new Cas30ServiceTicketValidator(Options, httpClient);
            }
        }

        protected override AuthenticationHandler<CasAuthenticationOptions> CreateHandler()
        {
            return new CasAuthenticationHandler();
        }
    }
}
