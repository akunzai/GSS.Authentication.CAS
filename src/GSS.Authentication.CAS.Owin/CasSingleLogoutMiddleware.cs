using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace GSS.Authentication.CAS.Owin
{
    /// <summary>
    /// Handles the CAS back-channel Single Sign-Out notification: parses the SAML-like <c>logoutRequest</c> the
    /// CAS server POSTs, and removes the matching entry (keyed by the CAS service ticket, i.e. SAML
    /// <c>SessionIndex</c>) from <see cref="IAuthenticationSessionStore"/>.
    /// </summary>
    /// <remarks>
    /// For this to actually end an already-signed-in cookie session, the same
    /// <see cref="IAuthenticationSessionStore"/> instance must also be assigned to
    /// <see cref="CookieAuthenticationOptions.SessionStore"/>, and <see cref="CasAuthenticationOptions.SaveTokens"/>
    /// must be <see langword="true"/> (so the service ticket is available to key the store by). When wired that
    /// way, removing the store entry takes effect on the very next request that presents the cookie — cookie
    /// authentication looks the ticket up in the store on every request, so there's no polling interval or
    /// caching delay to wait out.
    /// <para>
    /// Without that wiring (the default), the cookie itself carries the full encrypted ticket rather than a
    /// store key, so cookie authentication never consults the store at all — removing an entry here has no
    /// effect on an already-issued cookie, which then simply remains valid until it expires or is re-issued.
    /// </para>
    /// </remarks>
    public class CasSingleLogoutMiddleware : OwinMiddleware
    {
        private const string RequestContentType = "application/x-www-form-urlencoded";
        private const string LogoutRequest = "logoutRequest";
        private static readonly XmlNamespaceManager _xmlNamespaceManager = InitializeXmlNamespaceManager();
        private readonly IAuthenticationSessionStore _store;
        private readonly ILogger _logger;
        private readonly CasSingleLogoutOptions _options;

        public CasSingleLogoutMiddleware(
            OwinMiddleware next,
            IAppBuilder app,
            IAuthenticationSessionStore store,
            CasSingleLogoutOptions? options = null
        ) : base(next)
        {
            _logger = app.CreateLogger<CasSingleLogoutMiddleware>();
            _store = store;
            _options = options ?? new CasSingleLogoutOptions();
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.ContentType, RequestContentType, StringComparison.OrdinalIgnoreCase)
                && _options.IsTrustedRequest(context))
            {
                var formData = await context.Request.ReadFormAsync().ConfigureAwait(false);
                var logoutRequest = formData.FirstOrDefault(x => x.Key == LogoutRequest).Value?[0] ?? string.Empty;
                if (!string.IsNullOrEmpty(logoutRequest))
                {
                    var serviceTicket = ExtractServiceTicketFromLogoutRequest(logoutRequest);
                    if (!string.IsNullOrEmpty(serviceTicket))
                    {
                        await _store.RemoveAsync(serviceTicket).ConfigureAwait(false);
                    }
                }
            }

            await Next.Invoke(context).ConfigureAwait(false);
        }

        private string ExtractServiceTicketFromLogoutRequest(string text)
        {
            try
            {
                var doc = XDocument.Parse(text);
                var nav = doc.CreateNavigator();
                /*
                <samlp:LogoutRequest 
                xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol" 
                xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion" 
                ID="[RANDOM ID]" 
                Version="2.0" 
                IssueInstant="[CURRENT DATE/TIME]">
                  <saml:NameID>@NOT_USED@</saml:NameID>
                  <samlp:SessionIndex>[SESSION IDENTIFIER]</samlp:SessionIndex>
                </samlp:LogoutRequest>
                */
                var node = nav.SelectSingleNode("samlp:LogoutRequest/samlp:SessionIndex/text()", _xmlNamespaceManager);
                if (node != null)
                {
                    return node.Value;
                }
            }
            catch (XmlException e)
            {
                _logger.WriteWarning(e.Message, e);
            }

            return string.Empty;
        }

        private static XmlNamespaceManager InitializeXmlNamespaceManager()
        {
            var namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            return namespaceManager;
        }
    }
}