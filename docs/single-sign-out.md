# Sign-out and Single Sign-Out

Two separate things: the user signing out of *this* application, and CAS telling the application that a session
ended somewhere else.

## Sign-out

Signing out of the CAS scheme redirects to the CAS `/logout` endpoint and back to `SignedOutCallbackPath`
(`/signout-callback-cas` by default), which then forwards to `SignedOutRedirectUri` (`/` by default).

`OnRedirectToIdentityProviderForSignOut` can rewrite that redirect, or take it over entirely by calling
`HandleResponse()`.

```csharp
options.Events.OnRedirectToIdentityProviderForSignOut = context =>
{
    context.RedirectUri = QueryHelpers.AddQueryString(context.RedirectUri, "locale", "zh_TW");
    return Task.CompletedTask;
};
```

## Single Sign-Out

CAS notifies the application out-of-band when a session ends elsewhere, by POSTing a SAML-like `logoutRequest`
naming the service ticket. `CasSingleLogoutMiddleware` parses it and removes the matching entry from the ticket
store.

For that removal to actually end a signed-in cookie session, the same store must also be the cookie handler's
`SessionStore`, and `SaveTokens` must be on so the service ticket is available to key it by:

```csharp
builder.Services.AddDistributedMemoryCache();   // or AddStackExchangeRedisCache for multiple instances
builder.Services.AddSingleton<ITicketStore, DistributedCacheTicketStore>();
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((o, t) => o.SessionStore = t);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie()
    .AddCAS(options =>
    {
        options.CasServerUrlBase = "https://cas.example.org/cas";
        options.SaveTokens = true;
    });
// ...
app.UseCasSingleLogout();
```

Removal takes effect on the very next request presenting the cookie — cookie authentication looks the ticket up
in the store on every request, so there is no polling interval or caching delay to wait out.

Without that wiring, the cookie carries the full encrypted ticket rather than a store key, so cookie
authentication never consults the store at all: removing an entry has no effect on an already-issued cookie,
which stays valid until it expires or is re-issued.

The OWIN equivalent uses `IAuthenticationSessionStore` and
`DistributedCacheIAuthenticationSessionStore`; see [`owin/OwinSample`](../owin/OwinSample/).

### Restricting who is trusted

The notification carries no signature or authentication of its own — the CAS protocol defines it as "fire and
forget" (§2.3.3). `IsTrustedRequest` is a client-side hardening hook, not a protocol guarantee: it lets you
reject anything that did not come from your CAS server before the ticket store is touched. It defaults to
trusting every request.

```csharp
var trustedAddresses = await Dns.GetHostAddressesAsync(new Uri(casServerUrlBase).Host);
app.UseCasSingleLogout(options: new CasSingleLogoutOptions
{
    IsTrustedRequest = context => context.Connection.RemoteIpAddress != null
        && trustedAddresses.Contains(context.Connection.RemoteIpAddress)
});
```

Resolving the addresses once at startup, as above, keeps a DNS lookup off the request path. Behind a reverse
proxy, configure forwarded headers so `RemoteIpAddress` is the real client — or check something else entirely.
