using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class CasRedirectToAuthorizationEndpointContext : BaseCasContext
    {
        public CasRedirectToAuthorizationEndpointContext(HttpContext context, CasAuthenticationOptions options,
            AuthenticationProperties properties, string redirectUri)
            : base(context, options)
        {
            RedirectUri = redirectUri;
            Properties = properties;
        }

        public string RedirectUri { get; private set; }

        public AuthenticationProperties Properties { get; private set; }
    }
}
