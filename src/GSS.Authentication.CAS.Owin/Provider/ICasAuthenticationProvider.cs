using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSS.Authentication.CAS.Owin
{
    public interface ICasAuthenticationProvider
    {
        Task CreatingTicket(CasCreatingTicketContext context);
        Task RedirectToAuthorizationEndpoint(CasRedirectToAuthorizationEndpointContext context);
    }
}
