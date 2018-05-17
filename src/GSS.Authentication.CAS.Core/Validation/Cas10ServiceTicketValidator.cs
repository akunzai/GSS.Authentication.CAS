using System.Net.Http;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS.Validation
{
    // see https://apereo.github.io/cas/5.2.x/protocol/CAS-Protocol-Specification.html#24-validate-cas-10
    public class Cas10ServiceTicketValidator : CasServiceTicketValidator
    {
        public Cas10ServiceTicketValidator(
            ICasOptions options,
            HttpClient httpClient = null) 
            : base(options, httpClient)
        {
            ValidateUrlSuffix = "validate";
        }

        protected override ICasPrincipal BuildPrincipal(string responseBody)
        {
            var responseParts = responseBody.Split('\n');
            if (responseParts.Length >= 2 && responseParts[0] == "yes")
            {
                var assertion = new Assertion(responseParts[1]);
                return new CasPrincipal(assertion, options.AuthenticationType);
            }
            return null;
        }
    }
}
