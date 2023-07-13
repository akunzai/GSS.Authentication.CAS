using System;
using System.Net.Http;
using System.Security.Claims;
using GSS.Authentication.CAS.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace GSS.Authentication.CAS.AspNetCore;

public class CasCreatingTicketContext : ResultContext<CasAuthenticationOptions>
{
    public CasCreatingTicketContext(
        ClaimsPrincipal principal,
        AuthenticationProperties properties,
        HttpContext context,
        AuthenticationScheme scheme,
        CasAuthenticationOptions options,
        HttpClient backchannel,
        Assertion assertion)
        : base(context, scheme, options)
    {
        Backchannel = backchannel ?? throw new ArgumentNullException(nameof(backchannel));
        Assertion = assertion ?? throw new ArgumentNullException(nameof(assertion));
        Principal = principal;
        Properties = properties;
    }

    public Assertion Assertion { get; }

    /// <summary>
    /// Gets the backchannel used to communicate with the provider.
    /// </summary>
    public HttpClient Backchannel { get; }

    /// <summary>
    /// Gets the main identity exposed by the authentication ticket.
    /// This property returns <c>null</c> when the ticket is <c>null</c>.
    /// </summary>
    public ClaimsIdentity? Identity => Principal?.Identity as ClaimsIdentity;
}