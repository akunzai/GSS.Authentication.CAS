using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace GSS.Authentication.CAS.Owin
{
    public interface ICasAuthenticationProvider
    {
        Task CreatingTicket(CasCreatingTicketContext context);

        string GetPublicOrigin(IOwinContext context);

        Task RedirectToAuthorizationEndpoint(CasRedirectToAuthorizationEndpointContext context);
    }
}
