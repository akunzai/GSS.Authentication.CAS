using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace GSS.Authentication.CAS.Owin
{
    public class CasSingleLogoutMiddleware : OwinMiddleware
    {
        private const string RequestContentType = "application/x-www-form-urlencoded";
        private const string LogoutRequest = "logoutRequest";
        private static readonly XmlNamespaceManager _xmlNamespaceManager = InitializeXmlNamespaceManager();
        private readonly IAuthenticationSessionStore _store;

        public CasSingleLogoutMiddleware(
            OwinMiddleware next,
            IAppBuilder app,
            IAuthenticationSessionStore store
            ) : base(next)
        {
            _store = store;
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.ContentType, RequestContentType, StringComparison.OrdinalIgnoreCase))
            {
                var formData = await context.Request.ReadFormAsync().ConfigureAwait(false);
                var logoutRequest = formData.FirstOrDefault(x => x.Key == LogoutRequest).Value?[0] ?? string.Empty;
                if (!string.IsNullOrEmpty(logoutRequest))
                {
                    var serviceTicket = ExtractSingleSignOutTicketFromSamlResponse(logoutRequest);
                    if (!string.IsNullOrEmpty(serviceTicket))
                    {
                        await _store.RemoveAsync(serviceTicket).ConfigureAwait(false);
                    }
                }
            }
            await Next.Invoke(context).ConfigureAwait(false);
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
