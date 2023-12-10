using System;
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

    public IServiceTicketValidator? ServiceTicketValidator { get; set; }

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
    }
}