using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Handles the CAS back-channel Single Sign-Out notification: parses the SAML-like <c>logoutRequest</c> the CAS
/// server POSTs, and removes the matching entry (keyed by the CAS service ticket, i.e. SAML <c>SessionIndex</c>)
/// from <see cref="ITicketStore"/>.
/// </summary>
/// <remarks>
/// For this to actually end an already-signed-in cookie session, the same <see cref="ITicketStore"/> instance
/// must also be assigned to <see cref="CookieAuthenticationOptions.SessionStore"/>, and
/// <c>CasAuthenticationOptions.SaveTokens</c> must be <see langword="true"/> (so the service ticket is
/// available to key the store by). When wired that way, removing the store entry takes effect on the very next
/// request that presents the cookie — cookie authentication looks the ticket up in the store on every request,
/// so there's no polling interval or caching delay to wait out.
/// <para>
/// Without that wiring (the default), the cookie itself carries the full encrypted ticket rather than a store
/// key, so cookie authentication never consults the store at all — removing an entry here has no effect on an
/// already-issued cookie, which then simply remains valid until it expires or is re-issued.
/// </para>
/// </remarks>
public class CasSingleLogoutMiddleware
{
    private const string RequestContentType = "application/x-www-form-urlencoded";
    private const string LogoutRequest = "logoutRequest";
    private static readonly XmlNamespaceManager _xmlNamespaceManager = InitializeXmlNamespaceManager();
    private readonly ITicketStore _store;
    private readonly RequestDelegate _next;
    private readonly ILogger<CasSingleLogoutMiddleware> _logger;
    private readonly CasSingleLogoutOptions _options;

    public CasSingleLogoutMiddleware(RequestDelegate next, ITicketStore store,
        ILogger<CasSingleLogoutMiddleware> logger, IOptions<CasSingleLogoutOptions>? options = null)
    {
        _next = next;
        _store = store;
        _logger = logger;
        _options = options?.Value ?? new CasSingleLogoutOptions();
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase)
            && string.Equals(context.Request.ContentType, RequestContentType, StringComparison.OrdinalIgnoreCase)
            && _options.IsTrustedRequest(context))
        {
            var formData = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            if (formData.ContainsKey(LogoutRequest))
            {
                var logoutRequest = formData.First(x => x.Key == LogoutRequest).Value[0];
                if (!string.IsNullOrEmpty(logoutRequest))
                {
                    var serviceTicket = ExtractServiceTicketFromLogoutRequest(logoutRequest);
                    if (!string.IsNullOrEmpty(serviceTicket))
                    {
                        await _store.RemoveAsync(serviceTicket).ConfigureAwait(false);
                    }
                }
            }
        }

        await _next.Invoke(context).ConfigureAwait(false);
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
            _logger.LogWarning(e, "{Exception}", e.Message);
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