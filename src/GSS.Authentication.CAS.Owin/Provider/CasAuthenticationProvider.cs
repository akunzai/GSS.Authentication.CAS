using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace GSS.Authentication.CAS.Owin
{
    public class CasAuthenticationProvider : ICasAuthenticationProvider
    {
        public Func<CasCreatingTicketContext, Task> OnCreatingTicket { get; set; } = context => Task.FromResult(0);

        public Func<IOwinContext, string> OnGetPublicOrigin { get; set; } = context =>
            $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}";

        public Func<CasRedirectToAuthorizationEndpointContext, Task> OnRedirectToAuthorizationEndpoint { get; set; } = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.FromResult(0);
        };
        
        public virtual Task CreatingTicket(CasCreatingTicketContext context) => OnCreatingTicket(context);

        public string GetPublicOrigin(IOwinContext context) => OnGetPublicOrigin(context);

        public virtual Task RedirectToAuthorizationEndpoint(CasRedirectToAuthorizationEndpointContext context) => OnRedirectToAuthorizationEndpoint(context);
    }
}
