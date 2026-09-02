using System;
using Microsoft.AspNetCore.Http;

namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Configuration options for <see cref="CasSingleLogoutMiddleware"/>.
/// </summary>
public class CasSingleLogoutOptions
{
    /// <summary>
    /// Called for every incoming request before it's treated as a genuine CAS back-channel logout notification.
    /// Return <see langword="false"/> to ignore the request without removing anything from the ticket store.
    /// </summary>
    /// <remarks>
    /// The CAS protocol defines the back-channel logout as "fire and forget" with no signature or authentication
    /// mechanism of its own (CAS Protocol Specification §2.3.3), so this is a client-side hardening hook, not a
    /// protocol guarantee. Defaults to trusting every request, matching prior behavior.
    /// <para>
    /// Example: restrict to requests originating from the configured CAS server's host, resolved once at startup:
    /// <code>
    /// var trustedAddresses = await Dns.GetHostAddressesAsync(new Uri(casServerUrlBase).Host);
    /// options.IsTrustedRequest = context =&gt;
    ///     context.Connection.RemoteIpAddress != null &amp;&amp; trustedAddresses.Contains(context.Connection.RemoteIpAddress);
    /// </code>
    /// </para>
    /// </remarks>
    public Func<HttpContext, bool> IsTrustedRequest { get; set; } = _ => true;
}
