using System.Threading.Tasks;
using Microsoft.Owin.Security;

// ReSharper disable once CheckNamespace
namespace GSS.Authentication.CAS.Owin
{
    /// <summary>
    /// Specifies callback methods which the <see cref="CasAuthenticationMiddleware"></see> invokes to enable developer control over the authentication process. />
    /// </summary>
    public interface ICasAuthenticationProvider
    {
        /// <summary>
        /// Invoked after the provider successfully authenticates a user.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        Task CreatingTicket(CasCreatingTicketContext context);

        /// <summary>
        /// Called when a Challenge causes a redirect to authorize endpoint in the CAS handler.
        /// </summary>
        /// <param name="context">Contains redirect URI and <see cref="AuthenticationProperties"/> of the challenge.</param>
        /// <returns></returns>
        Task RedirectToAuthorizationEndpoint(CasRedirectToAuthorizationEndpointContext context);
        
        /// <summary>
        /// Invoked before redirecting to the identity provider to sign out.
        /// </summary>
        Task RedirectToIdentityProviderForSignOut(CasRedirectContext context);

        /// <summary>
        /// Invoked when there is a remote failure.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task RemoteFailure(CasRemoteFailureContext context);
    }
}
