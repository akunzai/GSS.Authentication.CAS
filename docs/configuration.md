# Configuration

How to register the middleware, choose a CAS protocol version, and turn a validated assertion into claims.

## Registration

ASP.NET Core:

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie()
    .AddCAS(options => options.CasServerUrlBase = "https://cas.example.org/cas");
```

`AddCAS` also takes an explicit authentication scheme and display name when you need more than one CAS server.

OWIN:

```csharp
app.UseCasAuthentication(new CasAuthenticationOptions
{
    CasServerUrlBase = "https://cas.example.org/cas"
});
```

## Protocol versions

CAS 1.0, 2.0 and 3.0 are supported. CAS 1.0 returns the principal name only; 2.0 and 3.0 also carry released
attributes.

| Protocol | Validator | Endpoint |
| -------- | --------- | -------- |
| CAS 1.0 | `Cas10ServiceTicketValidator` | `/validate` |
| CAS 2.0 | `Cas20ServiceTicketValidator` | `/serviceValidate` |
| CAS 3.0 | `Cas30ServiceTicketValidator` | `/p3/serviceValidate` |

On ASP.NET Core, the handler picks one from the `CAS:ProtocolVersion` configuration value (`1`, `2` or `3`,
defaulting to `3`) unless you assign `ServiceTicketValidator` yourself. On OWIN there is no configuration
binding: CAS 3.0 is the default, and any other version means assigning the validator explicitly.

```csharp
options.ServiceTicketValidator = new Cas20ServiceTicketValidator(options);
```

### JSON validation responses

CAS 2.0/3.0 responses are parsed as JSON whenever the server replies with `application/json` (the CAS 3.0
`format=JSON` output), and as XML otherwise. No configuration is required.

Non-string attribute values are kept as their raw JSON text, since CAS servers do release them — Apereo CAS
sends `"isFromNewLogin":[true]`, for instance.

## Claims mapping

The validated `Assertion` — principal name plus released attributes — is handed to `OnCreatingTicket` so you
decide which claims to issue.

```csharp
options.Events.OnCreatingTicket = context =>
{
    var assertion = context.Assertion;
    context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
    if (assertion.Attributes.TryGetValue("email", out var email))
    {
        context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email!));
    }

    return Task.CompletedTask;
};
```

`OnRemoteFailure` covers validation failures. `IPrincipal.GetPrincipalName()` reads the principal name back
from either a CAS principal or a plain `ClaimsPrincipal`.

## Login redirect parameters

`Renew`, `Gateway` (mutually exclusive), `Method` and `Locale` map to the CAS `/login` query parameters.

```csharp
.AddCAS(options =>
{
    options.CasServerUrlBase = "https://cas.example.org/cas";
    options.Gateway = true;      // transparent SSO check, no credential prompt
    options.Locale = "zh_TW";
});
```

| Option | CAS parameter | Effect |
| ------ | ------------- | ------ |
| `Renew` | `renew=true` | Force re-authentication with primary credentials, ignoring an existing SSO session |
| `Gateway` | `gateway=true` | Transparent check: never prompt, redirect straight back without a `ticket` if there is no SSO session |
| `Method` | `method` | How CAS delivers the response to `service` (e.g. `POST`, `HEADER`); unset means a `GET` redirect |
| `Locale` | `locale` | Locale hint for the CAS login page; not core protocol, but widely supported |

Setting both `Renew` and `Gateway` throws at startup, per the CAS protocol.
