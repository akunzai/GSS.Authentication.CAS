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

namespace GSS.Authentication.CAS.AspNetCore
{
    public class CasSingleLogoutMiddleware
    {
        private const string RequestContentType = "application/x-www-form-urlencoded";
        private const string LogoutRequest = "logoutRequest";
        private static readonly XmlNamespaceManager _xmlNamespaceManager = InitializeXmlNamespaceManager();
        private readonly ITicketStore _store;
        private readonly RequestDelegate _next;
        private readonly ILogger<CasSingleLogoutMiddleware> _logger;

        public CasSingleLogoutMiddleware(RequestDelegate next, ITicketStore store,
            ILogger<CasSingleLogoutMiddleware> logger)
        {
            _next = next;
            _store = store;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.ContentType, RequestContentType, StringComparison.OrdinalIgnoreCase))
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
                _logger.LogWarning(e, e.Message);
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