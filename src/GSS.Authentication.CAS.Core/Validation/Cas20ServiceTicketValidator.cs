using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Xml.Linq;
using GSS.Authentication.CAS.Security;
using Microsoft.Extensions.Primitives;

namespace GSS.Authentication.CAS.Validation
{
    // see https://apereo.github.io/cas/development/protocol/CAS-Protocol-Specification.html#25-servicevalidate-cas-20
    public class Cas20ServiceTicketValidator : CasServiceTicketValidator
    {
        private static readonly XNamespace _namespace = "http://www.yale.edu/tp/cas";
        private static readonly XName _attributes = _namespace + "attributes";
        private static readonly XName _authenticationSuccess = _namespace + "authenticationSuccess";
        private static readonly XName _authenticationFailure = _namespace + "authenticationFailure";
        private static readonly XName _user = _namespace + "user";

        public Cas20ServiceTicketValidator(
            ICasOptions options,
            HttpClient? httpClient = null)
            : base("serviceValidate", options, httpClient)
        {
        }

        protected Cas20ServiceTicketValidator(
            string suffix,
            ICasOptions options,
            HttpClient? httpClient = null)
            : base(suffix, options, httpClient)
        {
        }

        protected override ICasPrincipal? BuildPrincipal(string responseBody)
        {
            var doc = XElement.Parse(responseBody);
            /* On ticket validation failure:
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
             <cas:authenticationFailure code="INVALID_TICKET">
                Ticket ST-1856339-aA5Yuvrxzpv8Tau1cYQ7 not recognized
              </cas:authenticationFailure>
            </cas:serviceResponse>
            */
            var failureElement = doc.Element(_authenticationFailure);
            if (failureElement != null)
            {
                throw new AuthenticationException(failureElement.Value);
            }

            /* On ticket validation success
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
                <cas:authenticationSuccess>
                <cas:user>username</cas:user>
                <cas:proxyGrantingTicket>PGTIOU-84678-8a9d...</cas:proxyGrantingTicket>
                </cas:authenticationSuccess>
            </cas:serviceResponse>
            */
            var successElement = doc.Element(_authenticationSuccess);
            if (successElement == null)
                return null;
            var principalName = successElement.Element(_user)?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(principalName))
                return null;
            var attributes = new Dictionary<string, StringValues>();
            var attributeElements = successElement.Element(_attributes)?.Elements();
            /* User attributes may released in CAS v2 protocol with forward-compatible mode
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
                <cas:authenticationSuccess>
                  <cas:user>username</cas:user>
                  <cas:attributes>
                    <cas:firstname>John</cas:firstname>
                    <cas:lastname>Doe</cas:lastname>
                    <cas:title>Mr.</cas:title>
                    <cas:email>jdoe @example.org</cas:email>
                    <cas:affiliation>staff</cas:affiliation>
                    <cas:affiliation>faculty</cas:affiliation>
                  </cas:attributes>
                  <cas:proxyGrantingTicket>PGTIOU-84678-8a9d...</cas:proxyGrantingTicket>
                </cas:authenticationSuccess>
            </cas:serviceResponse>
             */
            if (attributeElements != null)
            {
                foreach (var attr in attributeElements)
                {
                    var name = attr.Name.LocalName;
                    attributes[name] = attributes.ContainsKey(name)
                        ? StringValues.Concat(attributes[name], attr.Value)
                        : new StringValues(attr.Value);
                }
            }

            var assertion = new Assertion(principalName, attributes);
            return new CasPrincipal(assertion, Options.AuthenticationType);
        }
    }
}