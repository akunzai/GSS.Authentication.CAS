using System.Net.Http;

namespace GSS.Authentication.CAS.Validation
{
    // see https://apereo.github.io/cas/development/protocol/CAS-Protocol-Specification.html#29-p3proxyvalidate-cas-30
    public class Cas30ProxyTicketValidator : Cas20ServiceTicketValidator
    {
        public Cas30ProxyTicketValidator(
            ICasOptions options,
            HttpClient? httpClient = null)
            : base("p3/proxyValidate", options, httpClient)
        {
        }
    }
}
