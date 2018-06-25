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
    public class CasSingleSignOutMiddleware : OwinMiddleware
    {
        protected static XmlNamespaceManager xmlNamespaceManager;
        private const string RequestContentType = "application/x-www-form-urlencoded";
        private readonly ILogger logger;

        static CasSingleSignOutMiddleware()
        {
            xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
        }

        protected IAuthenticationSessionStore store;
        public CasSingleSignOutMiddleware(
            OwinMiddleware next,
            IAppBuilder app,
            IAuthenticationSessionStore store
            ) : base(next)
        {
            this.store = store;
            logger = app.CreateLogger<CasSingleSignOutMiddleware>();
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase)
                && context.Request.ContentType.Equals(RequestContentType, StringComparison.OrdinalIgnoreCase))
            {
                var formData = await context.Request.ReadFormAsync().ConfigureAwait(false);
                var logOutRequest = formData.FirstOrDefault(x => x.Key == "logoutRequest").Value?[0];
                if (!string.IsNullOrEmpty(logOutRequest))
                {
                    logger.WriteVerbose($"logOutRequest: {logOutRequest}");
                    var servieTicket = ExtractSingleSignOutTicketFromSamlResponse(logOutRequest);
                    if (!string.IsNullOrEmpty(servieTicket))
                    {
                        logger.WriteInformation($"removing ServiceTicket: {servieTicket} ... from[{context.Request.RemoteIpAddress}]");
                        await store.RemoveAsync(servieTicket).ConfigureAwait(false);
                    }
                }
            }
            await Next.Invoke(context).ConfigureAwait(false);
        }

        protected string ExtractSingleSignOutTicketFromSamlResponse(string text)
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
                var node = nav.SelectSingleNode("samlp:LogoutRequest/samlp:SessionIndex/text()", xmlNamespaceManager);
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
    }
}
