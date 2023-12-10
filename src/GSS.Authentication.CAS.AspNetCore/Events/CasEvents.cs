using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

// ReSharper disable once CheckNamespace
namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Default CAS events implementation.
/// </summary>
public class CasEvents : RemoteAuthenticationEvents
{
    /// <summary>
    /// Gets or sets the function that is invoked when the CreatingTicket method is invoked.
    /// </summary>
    public Func<CasCreatingTicketContext, Task> OnCreatingTicket { get; set; } = _ => Task.CompletedTask;

    /// <summary>
    /// Gets or sets the delegate that is invoked when the RedirectToAuthorizationEndpoint method is invoked.
    /// </summary>
    public Func<RedirectContext<CasAuthenticationOptions>, Task> OnRedirectToAuthorizationEndpoint { get; set; } = context =>
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    /// <summary>
    /// Invoked before redirecting to the identity provider to sign out.
    /// </summary>
    public Func<CasRedirectContext, Task> OnRedirectToIdentityProviderForSignOut { get; set; } = _ => Task.CompletedTask;

    /// <summary>
    /// Invoked after the provider successfully authenticates a user.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
    public virtual Task CreatingTicket(CasCreatingTicketContext context) => OnCreatingTicket(context);

    /// <summary>
    /// Called when a Challenge causes a redirect to authorize endpoint in the OAuth handler.
    /// </summary>
    /// <param name="context">Contains redirect URI and <see cref="AuthenticationProperties"/> of the challenge.</param>
    /// <returns></returns>
    public virtual Task RedirectToAuthorizationEndpoint(RedirectContext<CasAuthenticationOptions> context) => OnRedirectToAuthorizationEndpoint(context);
    
    /// <summary>
    /// Invoked before redirecting to the identity provider to sign out.
    /// </summary>
    public virtual Task RedirectToIdentityProviderForSignOut(CasRedirectContext context) => OnRedirectToIdentityProviderForSignOut(context);
}