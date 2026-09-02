# Proxy tickets (CAS 2.0/3.0)

Proxying lets this application call a back-end service *as the signed-in user*, without holding their
credentials. CAS issues a Proxy Granting Ticket (PGT) during ticket validation; the application later exchanges
it for a Proxy Ticket (PT) scoped to one target service.

See CAS Protocol Specification §2.5.4 (`pgtUrl`), §2.6 (`/proxyValidate`) and §2.7 (`/proxy`).

## Requesting a Proxy Granting Ticket

Set `ProxyCallbackPath` and the handler adds `pgtUrl` to its validation request.

```csharp
.AddCAS(options =>
{
    options.CasServerUrlBase = "https://cas.example.org/cas";
    // must be reachable by the CAS server over HTTPS
    options.ProxyCallbackPath = "/proxy-callback-cas";
    // default is single-process; supply a distributed store for multi-instance deployments,
    // since the PGTIOU callback and the validation response may land on different instances
    options.ProxyGrantingTicketStore = new InMemoryProxyGrantingTicketStore();
});
```

Two things arrive separately and have to be correlated:

- the **PGTIOU** comes back inside the validation response, and lands on `Assertion.ProxyGrantingTicketIou`
- the real **PGT** is delivered by CAS in a separate call to `ProxyCallbackPath`

`IProxyGrantingTicketStore` is what ties them together. The default `InMemoryProxyGrantingTicketStore` is
single-process; behind a load balancer the callback and the validation response can land on different
instances, so implement the interface over a distributed cache there.

> [!IMPORTANT]
> CAS will not issue a PGT to a non-HTTPS `pgtUrl`. If the resulting URL is not HTTPS the handler logs a
> warning and CAS silently declines.

## Getting a Proxy Ticket

```csharp
var provider = new CasProxyTicketProvider(options);
var proxyTicket = await provider.GetProxyTicketAsync(proxyGrantingTicket, "https://backend.example.org/api");
```

Pass the ticket to the target service as the `ticket` query parameter, exactly as CAS would.

## Validating an incoming Proxy Ticket

A service receiving a PT validates it against a proxy endpoint rather than the plain service endpoint:

| Validator | Endpoint |
| --------- | -------- |
| `Cas20ProxyTicketValidator` | `/proxyValidate` |
| `Cas30ProxyTicketValidator` | `/p3/proxyValidate` |

```csharp
options.ServiceTicketValidator = new Cas30ProxyTicketValidator(options);
```

`Assertion.Proxies` then carries the chain of proxying services, most-recently-visited first. Check it if the
service should only accept calls proxied through known intermediaries.

## Testing

The Keycloak CAS protocol extension used by this repository's E2E suite does not implement proxy tickets, so
`/proxy` and `/proxyValidate` are covered by unit tests only. Verify against a real Apereo CAS server before
relying on this in production.
