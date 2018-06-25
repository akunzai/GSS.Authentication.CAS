using System.Net.Http;
using System.Security.Authentication;
using System.Xml.Linq;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS.Validation
{
    // see https://apereo.github.io/cas/5.2.x/protocol/CAS-Protocol-Specification.html#25-servicevalidate-cas-20
    public class Cas20ServiceTicketValidator : CasServiceTicketValidator
    {
        protected static XNamespace Namespace = "http://www.yale.edu/tp/cas";
        protected static XName Attributes = Namespace + "attributes";
        protected static XName AuthenticationSuccess = Namespace + "authenticationSuccess";
        protected static XName AuthenticationFailure = Namespace + "authenticationFailure";
        protected static XName User = Namespace + "user";
        protected const string Code = "code";

        public Cas20ServiceTicketValidator(
            ICasOptions options,
            HttpClient httpClient = null)
            : base(options, httpClient)
        {
            ValidateUrlSuffix = "serviceValidate";
        }

        protected override ICasPrincipal BuildPrincipal(string responseBody)
        {
            var doc = XElement.Parse(responseBody);
            /* On ticket validation failure:
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
             <cas:authenticationFailure code="INVALID_TICKET">
                Ticket ST-1856339-aA5Yuvrxzpv8Tau1cYQ7 not recognized
              </cas:authenticationFailure>
            </cas:serviceResponse>
            */
            var failureElement = doc.Element(AuthenticationFailure);
            if (failureElement != null)
            {
                throw new AuthenticationException(failureElement.Value);
            }
            /* On ticket validation success:
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
                <cas:authenticationSuccess>
                <cas:user>username</cas:user>
                <cas:proxyGrantingTicket>PGTIOU-84678-8a9d...</cas:proxyGrantingTicket>
                </cas:authenticationSuccess>
            </cas:serviceResponse>
            */
            var principalName = doc.Element(AuthenticationSuccess).Element(User)?.Value;
            var assertion = new Assertion(principalName);
            return new CasPrincipal(assertion, options.AuthenticationType);
        }
    }
}
