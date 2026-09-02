using System.Net.Http;

namespace GSS.Authentication.CAS.Validation
{
    // see https://apereo.github.io/cas/development/protocol/CAS-Protocol-Specification.html#26-proxyvalidate-cas-20
    public class Cas20ProxyTicketValidator : Cas20ServiceTicketValidator
    {
        public Cas20ProxyTicketValidator(
            ICasOptions options,
            HttpClient? httpClient = null)
            : base("proxyValidate", options, httpClient)
        {
        }
    }
}
