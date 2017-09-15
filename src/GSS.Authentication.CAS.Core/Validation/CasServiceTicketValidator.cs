using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS.Validation
{
    public abstract class CasServiceTicketValidator : IServiceTicketValidator
    {
        protected string validateUrl;
        protected HttpClient httpClient;
        protected ICasOptions options;

        public CasServiceTicketValidator(ICasOptions options, HttpClient httpClient = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.httpClient = httpClient ?? new HttpClient();
        }

        protected string ValidateUrlSuffix { get; set; }

        public virtual async Task<ICasPrincipal> ValidateAsync(string ticket, string service, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ticket)) throw new ArgumentNullException(nameof(ticket));
            if (string.IsNullOrEmpty(service)) throw new ArgumentNullException(nameof(service));
            var baseUri = new Uri(options.CasServerUrlBase + (options.CasServerUrlBase.EndsWith("/") ? string.Empty : "/"));
            var validateUri = new Uri(baseUri, ValidateUrlSuffix);
            // unescape first to prevent double escape
            var escapedService = Uri.EscapeDataString(Uri.UnescapeDataString(service));
            var escapedTicket = Uri.EscapeDataString(ticket);
            var requestUri = $"{validateUri.AbsoluteUri}?service={escapedService}&ticket={escapedTicket}";
            var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return BuildPrincipal(responseBody);
        }

        protected abstract ICasPrincipal BuildPrincipal(string responseBody);
    }
}
