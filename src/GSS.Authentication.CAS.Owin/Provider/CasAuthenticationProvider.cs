using System;
using System.Threading.Tasks;

namespace GSS.Authentication.CAS.Owin
{
    /// <inheritdoc />
    /// <summary>
    /// Default <see cref="ICasAuthenticationProvider"/> implementation.
    /// </summary>
    public class CasAuthenticationProvider : ICasAuthenticationProvider
    {
        /// <summary>
        /// Gets or sets the function that is invoked when the CreatingTicket method is invoked.
        /// </summary>
        public Func<CasCreatingTicketContext, Task> OnCreatingTicket { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Gets or sets the delegate that is invoked when the RedirectToAuthorizationEndpoint method is invoked.
        /// </summary>
        public Func<CasRedirectToAuthorizationEndpointContext, Task> OnRedirectToAuthorizationEndpoint { get; set; } = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        public virtual Task CreatingTicket(CasCreatingTicketContext context) => OnCreatingTicket(context);

        public virtual Task RedirectToAuthorizationEndpoint(CasRedirectToAuthorizationEndpointContext context) => OnRedirectToAuthorizationEndpoint(context);
    }
}
