using System.Security.Claims;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Provider;

// ReSharper disable once CheckNamespace
namespace GSS.Authentication.CAS.Owin
{
    public class CasCreatingTicketContext : BaseContext
    {
        public CasCreatingTicketContext(
            IOwinContext context,
            ClaimsIdentity identity,
            AuthenticationProperties properties) : base(context)
        {
            Identity = identity;
            Properties = properties;
        }

        public ClaimsIdentity Identity { get; set; }

        public AuthenticationProperties Properties { get; set; }
    }
}
