using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Provider;

// ReSharper disable once CheckNamespace
namespace GSS.Authentication.CAS.Owin
{
    public class CasRedirectToAuthorizationEndpointContext : ReturnEndpointContext
    {
        public CasRedirectToAuthorizationEndpointContext(
            IOwinContext context,
            AuthenticationTicket? ticket) : base(context, ticket)
        {
        }
    }
}
