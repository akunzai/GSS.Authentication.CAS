using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GSS.Authentication.CAS.Proxy
{
    /// <summary>
    /// Default <see cref="IProxyTicketProvider"/> implementation, calling the CAS <c>/proxy</c> endpoint.
    /// See https://apereo.github.io/cas/development/protocol/CAS-Protocol-Specification.html#27-proxy-cas-20
    /// </summary>
    public class CasProxyTicketProvider : IProxyTicketProvider
    {
        private static readonly XNamespace _namespace = "http://www.yale.edu/tp/cas";
        private static readonly XName _proxySuccess = _namespace + "proxySuccess";
        private static readonly XName _proxyFailure = _namespace + "proxyFailure";
        private static readonly XName _proxyTicket = _namespace + "proxyTicket";

        private readonly ICasOptions _options;
        private readonly HttpClient _httpClient;

        public CasProxyTicketProvider(ICasOptions options, HttpClient? httpClient = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <inheritdoc />
        public async Task<string> GetProxyTicketAsync(string proxyGrantingTicket, string targetService,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(proxyGrantingTicket))
                throw new ArgumentNullException(nameof(proxyGrantingTicket));
            if (string.IsNullOrEmpty(targetService))
                throw new ArgumentNullException(nameof(targetService));

            var proxyUri = new Uri(_options.GetBaseUri(), "proxy");
            var requestUri =
                $"{proxyUri.AbsoluteUri}?pgt={Uri.EscapeDataString(proxyGrantingTicket)}&targetService={Uri.EscapeDataString(targetService)}";
            var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Failed to request a Proxy Ticket for target service [{targetService}] with error status [{(int)response.StatusCode}], please make sure your CAS server supports the proxy URI [{proxyUri}]");
            }

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = XElement.Parse(responseBody);

            var failureElement = doc.Element(_proxyFailure);
            if (failureElement != null)
            {
                throw new AuthenticationException(failureElement.Value);
            }

            var successElement = doc.Element(_proxySuccess);
            var proxyTicket = successElement?.Element(_proxyTicket)?.Value;
            if (string.IsNullOrWhiteSpace(proxyTicket))
            {
                throw new AuthenticationException(
                    $"CAS server returned an unrecognized response for the proxy request: {responseBody}");
            }

            return proxyTicket!;
        }
    }
}
