using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class CasCreatingTicketContext : BaseCasContext
    {
        public CasCreatingTicketContext(
            HttpContext context,
            CasAuthenticationOptions options)
            : base(context, options)
        {
        }
        public ClaimsPrincipal Principal { get; set; }
        public AuthenticationProperties Properties { get; set; }
    }
}
