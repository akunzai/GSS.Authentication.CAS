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
    public class CasSingleSignOutMiddleware
    {
        private const string RequestContentType = "application/x-www-form-urlencoded";
        private static readonly XmlNamespaceManager _xmlNamespaceManager = InitializeXmlNamespaceManager();
        private readonly ITicketStore _store;
        private readonly ILogger<CasSingleSignOutMiddleware> _logger;
        private readonly RequestDelegate _next;

        public CasSingleSignOutMiddleware(RequestDelegate next, ITicketStore store, ILogger<CasSingleSignOutMiddleware> logger)
        {
            _next = next;
            _store = store;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context?.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase) == true
                && string.Equals(context.Request.ContentType, RequestContentType, StringComparison.OrdinalIgnoreCase))
            {
                var formData = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
                if (formData.ContainsKey("logoutRequest")){
                    var logOutRequest = formData.First(x => x.Key == "logoutRequest").Value[0];
                    if (!string.IsNullOrEmpty(logOutRequest))
                    {
                        _logger.LogDebug($"logoutRequest: {logOutRequest}");
                        var serviceTicket = ExtractSingleSignOutTicketFromSamlResponse(logOutRequest);
                        if (!string.IsNullOrEmpty(serviceTicket))
                        {
                            _logger.LogInformation($"remove serviceTicket: {serviceTicket} ...");
                            await _store.RemoveAsync(serviceTicket).ConfigureAwait(false);
                        }
                    }
                }
            }
            await _next.Invoke(context).ConfigureAwait(false);
        }

        private static string ExtractSingleSignOutTicketFromSamlResponse(string text)
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
            catch (XmlException)
            {
                //logoutRequest was not xml
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
