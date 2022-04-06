using System.Net.Http;

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
    }
}