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

        protected CasServiceTicketValidator(string suffix, ICasOptions options, HttpClient? httpClient = null)
        {
#pragma warning disable CS0618
            ValidateUrlSuffix = suffix;
#pragma warning restore CS0618
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClient ?? new HttpClient();
        }

        protected ICasOptions Options { get; }

        [Obsolete("Use constructor parameter instead")]
        protected string ValidateUrlSuffix { get; }

        public virtual async Task<ICasPrincipal?> ValidateAsync(string ticket, string service, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ticket)) throw new ArgumentNullException(nameof(ticket));
            if (string.IsNullOrEmpty(service)) throw new ArgumentNullException(nameof(service));
            var baseUri = new Uri(Options.CasServerUrlBase + (Options.CasServerUrlBase.EndsWith("/", StringComparison.Ordinal) ? string.Empty : "/"));
#pragma warning disable CS0618
            var validateUri = new Uri(baseUri, ValidateUrlSuffix);
#pragma warning restore CS0618
            // unescape first to prevent double escape
            var escapedService = Uri.EscapeDataString(Uri.UnescapeDataString(service));
            var escapedTicket = Uri.EscapeDataString(ticket);
            var requestUri = new Uri($"{validateUri.AbsoluteUri}?service={escapedService}&ticket={escapedTicket}");
            var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to validate ticket [{ticket}] for service [{service}] with error status [{(int)response.StatusCode}], please make sure your CAS server support the validate URI [{validateUri}]");
            }
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return BuildPrincipal(responseBody);
        }

        protected abstract ICasPrincipal? BuildPrincipal(string responseBody);
    }
}
