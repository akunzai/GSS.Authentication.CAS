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

The documentation below covers the API; these runnable samples put it together end to end.

- [ASP.NET Core][sample-aspnetcore]
- [ASP.NET Core with React.js][sample-react]
- [ASP.NET Core Identity][sample-identity]
- [ASP.NET Core Blazor][sample-blazor]
- [ASP.NET Core MVC][sample-mvc]
- [OWIN][sample-owin]

## Documentation

- [Configuration][doc-configuration] — registration, CAS 1.0/2.0/3.0 protocol versions, JSON validation
  responses, claims mapping, and the `renew`/`gateway`/`method`/`locale` login parameters
- [Sign-out and Single Sign-Out][doc-slo] — the CAS `/logout` redirect, back-channel session invalidation via
  a ticket store, and restricting which requests are trusted
- [Proxy tickets][doc-proxy] — PGT/PGTIOU, the `pgtUrl` callback, and `/proxy` + `/proxyValidate`

<!-- Absolute URLs: this file ships as the NuGet package readme, where relative links do not resolve. -->
[sample-aspnetcore]: https://github.com/akunzai/GSS.Authentication.CAS/tree/main/samples/AspNetCoreSample
[sample-react]: https://github.com/akunzai/GSS.Authentication.CAS/tree/main/samples/AspNetCoreReactSample
[sample-identity]: https://github.com/akunzai/GSS.Authentication.CAS/tree/main/samples/AspNetCoreIdentitySample
[sample-blazor]: https://github.com/akunzai/GSS.Authentication.CAS/tree/main/samples/BlazorSample
[sample-mvc]: https://github.com/akunzai/GSS.Authentication.CAS/tree/main/samples/AspNetCoreMvcSample
[sample-owin]: https://github.com/akunzai/GSS.Authentication.CAS/tree/main/owin/OwinSample
[doc-configuration]: https://github.com/akunzai/GSS.Authentication.CAS/blob/main/docs/configuration.md
[doc-slo]: https://github.com/akunzai/GSS.Authentication.CAS/blob/main/docs/single-sign-out.md
[doc-proxy]: https://github.com/akunzai/GSS.Authentication.CAS/blob/main/docs/proxy-tickets.md

## FAQ

Before you ask questions, please check out these [issues](https://github.com/akunzai/GSS.Authentication.CAS/issues?q=is%3Aissue+label%3Aquestion), or read the [FAQ](https://github.com/akunzai/GSS.Authentication.CAS/wiki/FAQ) first.
