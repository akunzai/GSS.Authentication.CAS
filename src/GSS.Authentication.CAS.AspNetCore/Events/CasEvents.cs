using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class CasEvents : RemoteAuthenticationEvents
    {
        public Func<CasCreatingTicketContext, Task> OnCreatingTicket { get; set; } = context => Task.CompletedTask;

        public Func<RedirectContext<CasAuthenticationOptions>, Task> OnRedirectToAuthorizationEndpoint { get; set; } = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        public virtual Task CreatingTicket(CasCreatingTicketContext context) => OnCreatingTicket(context);

        public virtual Task RedirectToAuthorizationEndpoint(RedirectContext<CasAuthenticationOptions> context) => OnRedirectToAuthorizationEndpoint(context);
    }
}
