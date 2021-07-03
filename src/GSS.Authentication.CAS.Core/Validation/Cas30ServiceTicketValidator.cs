using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Xml.Linq;
using GSS.Authentication.CAS.Security;
using Microsoft.Extensions.Primitives;

namespace GSS.Authentication.CAS.Validation
{
    // see https://apereo.github.io/cas/development/protocol/CAS-Protocol-Specification.html#28-p3servicevalidate-cas-30
    public class Cas30ServiceTicketValidator : Cas20ServiceTicketValidator
    {
        public Cas30ServiceTicketValidator(
            ICasOptions options,
            HttpClient? httpClient = null)
            : base("p3/serviceValidate", options, httpClient)
        {
        }

        protected override ICasPrincipal? BuildPrincipal(string responseBody)
        {
            var doc = XElement.Parse(responseBody);
            var failureElement = doc.Element(AuthenticationFailure);
            if (failureElement != null)
            {
                throw new AuthenticationException(failureElement.Value);
            }
            /* On ticket validation success:
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
            var successElement = doc.Element(AuthenticationSuccess);
            if (successElement == null) return null;
            var principalName = successElement.Element(User)?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(principalName)) return null;
            var attributes = new Dictionary<string, StringValues>();
            var attributeElements = successElement.Element(Attributes)?.Elements();
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