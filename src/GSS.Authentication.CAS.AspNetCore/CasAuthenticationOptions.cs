using System;
using GSS.Authentication.CAS.Proxy;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Configuration options for <see cref="CasAuthenticationHandler"/>
/// </summary>
public class CasAuthenticationOptions : RemoteAuthenticationOptions, ICasOptions
{
    public CasAuthenticationOptions()
    {
        CallbackPath = "/signin-cas";
        SignedOutCallbackPath = "/signout-callback-cas";
        Events = new CasEvents();
    }

    public string CasServerUrlBase { get; set; } = default!;

    public string AuthenticationType => CasDefaults.AuthenticationType;

    public IServiceTicketValidator ServiceTicketValidator { get; set; } = default!;

    public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; } = default!;

    /// <summary>
    /// The request path within the application's base path where the user agent will be returned after sign out from the CAS server.
    /// See service from https://apereo.github.io/cas/6.6.x/protocol/CAS-Protocol-Specification.html#231-parameters
    /// </summary>
    public PathString SignedOutCallbackPath { get; set; }

    /// <summary>
    /// The uri where the user agent will be redirected to after application is signed out from the identity provider.
    /// The redirect will happen after the SignedOutCallbackPath is invoked.
    /// </summary>
    /// <remarks>This URI can be out of the application's domain. By default it points to the root.</remarks>
    public string SignedOutRedirectUri { get; set; } = "/";

    /// <summary>
    /// The request path within the application's base path where CAS will deliver a Proxy Granting Ticket
    /// (<c>pgtId</c>/<c>pgtIou</c>) after successfully validating a service ticket for which a <c>pgtUrl</c> was
    /// requested. Must be reachable over HTTPS by the CAS server. Leave unset (the default) to not request PGTs.
    /// See CAS Protocol Specification §2.5.4/§3.3/§3.4.
    /// </summary>
    public PathString ProxyCallbackPath { get; set; }

    /// <summary>
    /// Correlates the PGTIOU returned in the validation response with the real Proxy Granting Ticket delivered to
    /// <see cref="ProxyCallbackPath"/>. Defaults to an in-memory, single-process store; provide a distributed
    /// implementation for multi-instance deployments.
    /// </summary>
    public IProxyGrantingTicketStore ProxyGrantingTicketStore { get; set; } = new InMemoryProxyGrantingTicketStore();

    /// <summary>
    /// Forces the user to re-authenticate with primary credentials, even if a valid SSO session already exists.
    /// Mutually exclusive with <see cref="Gateway"/>. See CAS Protocol Specification §2.1.1.
    /// </summary>
    public bool Renew { get; set; }

    /// <summary>
    /// Attempts a transparent/silent authentication: CAS will not prompt for credentials, redirecting straight
    /// back without a <c>ticket</c> if there's no existing SSO session. Mutually exclusive with <see cref="Renew"/>.
    /// See CAS Protocol Specification §2.1.1.
    /// </summary>
    public bool Gateway { get; set; }

    /// <summary>
    /// The CAS 3.0 <c>method</c> parameter, controlling how CAS delivers the response to <c>service</c>
    /// (e.g. <c>"POST"</c>, <c>"HEADER"</c>; the default, unset, is a <c>GET</c> redirect).
    /// See CAS Protocol Specification §2.1.1.
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// A hint for the locale CAS should render its login page in. Not part of the core CAS protocol, but widely
    /// supported by CAS server implementations.
    /// </summary>
    public string? Locale { get; set; }

    public new CasEvents Events
    {
        get => (CasEvents)base.Events;
        set => base.Events = value;
    }

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(CasServerUrlBase))
        {
            throw new ArgumentException($"The '{nameof(CasServerUrlBase)}' option must be provided.");
        }

        if (Renew && Gateway)
        {
            throw new ArgumentException(
                $"'{nameof(Renew)}' and '{nameof(Gateway)}' cannot both be set, per the CAS protocol.");
        }
    }
}
