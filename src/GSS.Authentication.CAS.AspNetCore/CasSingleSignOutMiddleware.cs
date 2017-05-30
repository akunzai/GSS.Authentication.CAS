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
        private readonly RequestDelegate next;

        protected ITicketStore store;
        protected ILogger<CasSingleSignOutMiddleware> logger;
        protected static XmlNamespaceManager xmlNamespaceManager;

        static CasSingleSignOutMiddleware()
        {
            xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
        }

        public CasSingleSignOutMiddleware(RequestDelegate next, ITicketStore store, ILogger<CasSingleSignOutMiddleware> logger)
        {
            this.next = next;
            this.store = store;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase))
            {
                var formData = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
                if (formData.ContainsKey("logoutRequest")){
                    var logOutRequest = formData.First(x => x.Key == "logoutRequest").Value[0];
                    if (!string.IsNullOrEmpty(logOutRequest))
                    {
                        logger.LogDebug($"logoutRequest: {logOutRequest}");
                        var serviceTicket = ExtractSingleSignOutTicketFromSamlResponse(logOutRequest);
                        if (!string.IsNullOrEmpty(serviceTicket))
                        {
                            logger.LogInformation($"remove serviceTicket: {serviceTicket} ...");
                            await store.RemoveAsync(serviceTicket);
                        }
                    }
                }
            }
            await next.Invoke(context);
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
