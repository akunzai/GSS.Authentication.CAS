using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Provider;

// ReSharper disable once CheckNamespace
namespace GSS.Authentication.CAS.Owin;

public class CasRedirectContext : ReturnEndpointContext
{
    public CasRedirectContext(IOwinContext context, AuthenticationTicket? ticket) : base(context, ticket)
    {
    }

    /// <summary>
    /// If true, will skip any default logic for this redirect.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Skips any default logic for this redirect.
    /// </summary>
    public void HandleResponse() => Handled = true;
}