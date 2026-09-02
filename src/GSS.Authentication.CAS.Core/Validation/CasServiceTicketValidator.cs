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
            CancellationToken cancellationToken = default,
            string? proxyCallbackUrl = null)
        {
            if (string.IsNullOrEmpty(ticket))
                throw new ArgumentNullException(nameof(ticket));
            if (string.IsNullOrEmpty(service))
                throw new ArgumentNullException(nameof(service));
            var validateUri = new Uri(Options.GetBaseUri(), _suffix);
            var requestUri =
                $"{validateUri.AbsoluteUri}?ticket={Uri.EscapeDataString(ticket)}&service={Uri.EscapeDataString(service)}";
            if (!string.IsNullOrEmpty(proxyCallbackUrl))
            {
                requestUri += $"&pgtUrl={Uri.EscapeDataString(proxyCallbackUrl)}";
            }
            var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Failed to validate ticket [{ticket}] for service [{service}] with error status [{(int)response.StatusCode}], please make sure your CAS server supports the validation URI [{validateUri}]");
            }

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return BuildPrincipal(responseBody, response.Content.Headers.ContentType?.MediaType);
        }

        /// <summary>
        /// Parses the validation response into a principal, or <see langword="null"/> if the response indicates
        /// the ticket didn't resolve to an authenticated user.
        /// </summary>
        /// <param name="responseBody">The raw response body from the CAS server.</param>
        /// <param name="contentType">
        /// The response's media type (e.g. <c>application/xml</c>, <c>application/json</c>), used by validators
        /// that support the CAS 3.0 <c>format</c> parameter to pick the right parser for whatever the server
        /// actually returned.
        /// </param>
        protected abstract ICasPrincipal? BuildPrincipal(string responseBody, string? contentType);
    }
}