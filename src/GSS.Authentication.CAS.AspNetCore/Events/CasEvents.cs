using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class CasEvents : RemoteAuthenticationEvents, ICasEvents
    {
        public Func<CasCreatingTicketContext, Task> OnCreatingTicket { get; set; } = context => Task.FromResult(0);

        public Func<CasRedirectToAuthorizationEndpointContext, Task> OnRedirectToAuthorizationEndpoint { get; set; } = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.FromResult(0);
        };

        public virtual Task CreatingTicket(CasCreatingTicketContext context) => OnCreatingTicket(context);

        public virtual Task RedirectToAuthorizationEndpoint(CasRedirectToAuthorizationEndpointContext context) => OnRedirectToAuthorizationEndpoint(context);
    }
}
