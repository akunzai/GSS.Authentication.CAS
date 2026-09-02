# GSS.Authentication.CAS

[![Build Status][build-badge]][build] [![Lint][lint-badge]][lint] [![Code Coverage][codecov-badge]][codecov]

[build]: https://github.com/akunzai/GSS.Authentication.CAS/actions/workflows/build.yml
[build-badge]: https://github.com/akunzai/GSS.Authentication.CAS/actions/workflows/build.yml/badge.svg
[lint]: https://github.com/akunzai/GSS.Authentication.CAS/actions/workflows/lint.yml
[lint-badge]: https://github.com/akunzai/GSS.Authentication.CAS/actions/workflows/lint.yml/badge.svg
[codecov]: https://codecov.io/gh/akunzai/GSS.Authentication.CAS
[codecov-badge]: https://codecov.io/gh/akunzai/GSS.Authentication.CAS/branch/main/graph/badge.svg?token=JGG7Y07SR0

CAS Authentication Middleware for OWIN & ASP.NET Core

## NuGet Packages

- [GSS.Authentication.CAS.Core ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.Core.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.Core/)
- [GSS.Authentication.CAS.Owin ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.Owin.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.Owin/)
- [GSS.Authentication.CAS.AspNetCore ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.AspNetCore.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.AspNetCore/)

## Installation

OWIN

```shell
dotnet add package GSS.Authentication.CAS.Owin
```

ASP.NET Core

```shell
dotnet add package GSS.Authentication.CAS.AspNetCore
```

## Usage

Currently, CAS protocol from 1.0 to 3.0 was supported.
Check out these [samples](./samples/) to learn the basics and key features.

- [ASP.NET Core](./samples/AspNetCoreSample/)
- [ASP.NET Core with React.js](./samples/AspNetCoreReactSample/)
- [ASP.NET Core Identity](./samples/AspNetCoreIdentitySample/)
- [ASP.NET Core Blazor](./samples/BlazorSample/)
- [ASP.NET Core MVC](./samples/AspNetCoreMvcSample/)
- [OWIN](./owin/OwinSample/)

## Key Features

### Login redirect parameters

`Renew`, `Gateway` (mutually exclusive), `Method` and `Locale` map to the CAS `/login` query parameters.

```csharp
.AddCAS(options =>
{
    options.CasServerUrlBase = "https://cas.example.org/cas";
    options.Gateway = true;      // transparent SSO check, no credential prompt
    options.Locale = "zh_TW";
});
```

### JSON validation responses

CAS 2.0/3.0 responses are parsed as JSON whenever the server replies with `application/json` (the CAS 3.0
`format=JSON` output), and as XML otherwise. No configuration is required.

### Proxy tickets (CAS 2.0/3.0)

Set `ProxyCallbackPath` to have the handler request a Proxy Granting Ticket during ticket validation, then use
`CasProxyTicketProvider` to obtain a Proxy Ticket for a back-end service.

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

Validate an incoming Proxy Ticket with `Cas20ProxyTicketValidator` (`/proxyValidate`) or
`Cas30ProxyTicketValidator` (`/p3/proxyValidate`); `Assertion.Proxies` then carries the proxy chain,
most-recently-visited first.

### Single Sign-Out hardening

CAS back-channel logout is unauthenticated by design ("fire and forget"). `IsTrustedRequest` lets you reject
notifications that don't originate from your CAS server before anything is removed from the ticket store.

```csharp
var trustedAddresses = await Dns.GetHostAddressesAsync(new Uri(casServerUrlBase).Host);
app.UseCasSingleLogout(options: new CasSingleLogoutOptions
{
    IsTrustedRequest = context => context.Connection.RemoteIpAddress != null
        && trustedAddresses.Contains(context.Connection.RemoteIpAddress)
});
```

## FAQ

Before you ask questions, please check out these [issues](https://github.com/akunzai/GSS.Authentication.CAS/issues?q=is%3Aissue+label%3Aquestion), or read the [FAQ](https://github.com/akunzai/GSS.Authentication.CAS/wiki/FAQ) first.
