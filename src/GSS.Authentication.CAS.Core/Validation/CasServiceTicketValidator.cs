using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS.Validation
{
    // see https://apereo.github.io/cas/development/protocol/CAS-Protocol-Specification.html
    public abstract class CasServiceTicketValidator : IServiceTicketValidator
    {
        private readonly HttpClient _httpClient;
        private readonly string _suffix;

        protected CasServiceTicketValidator(string suffix, ICasOptions options, HttpClient? httpClient = null)
        {
            _suffix = suffix;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClient ?? new HttpClient();
        }

        protected ICasOptions Options { get; }

        public virtual async Task<ICasPrincipal?> ValidateAsync(
            string ticket,
            string service,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(ticket))
                throw new ArgumentNullException(nameof(ticket));
            if (string.IsNullOrEmpty(service))
                throw new ArgumentNullException(nameof(service));
            var baseUri = new Uri(Options.CasServerUrlBase +
#if NETCOREAPP3_1_OR_GREATER
            (Options.CasServerUrlBase.EndsWith('/') 
#else
            (Options.CasServerUrlBase.EndsWith("/")
#endif
                                      ? string.Empty : "/"));
            var validateUri = new Uri(baseUri, _suffix);
            var requestUri =
                new Uri(
                    $"{validateUri.AbsoluteUri}?ticket={Uri.EscapeDataString(ticket)}&service={Uri.EscapeDataString(service)}");
            var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Failed to validate ticket [{ticket}] for service [{service}] with error status [{(int)response.StatusCode}], please make sure your CAS server supports the validation URI [{validateUri}]");
            }

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return BuildPrincipal(responseBody);
        }

        protected abstract ICasPrincipal? BuildPrincipal(string responseBody);
    }
}