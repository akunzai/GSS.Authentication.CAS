using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace GSS.Authentication.CAS.AspNetCore
{
    public interface ICasEvents : IRemoteAuthenticationEvents
    {
        Task CreatingTicket(CasCreatingTicketContext context);
        Task RedirectToAuthorizationEndpoint(CasRedirectToAuthorizationEndpointContext context);
    }
}
