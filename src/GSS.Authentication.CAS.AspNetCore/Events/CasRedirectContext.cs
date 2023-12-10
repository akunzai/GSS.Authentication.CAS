using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace GSS.Authentication.CAS.AspNetCore;

public class CasRedirectContext : RedirectContext<CasAuthenticationOptions>
{
    public CasRedirectContext(
        HttpContext context,
        AuthenticationScheme scheme,
        CasAuthenticationOptions options,
        AuthenticationProperties properties,
        string redirectUri) : base(context, scheme, options, properties, redirectUri)
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