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

- [.NET Aspire](./aspire/)
- [ASP.NET Core](./samples/AspNetCoreSample/)
- [ASP.NET Core with React.js](./samples/AspNetCoreReactSample/)
- [ASP.NET Core Identity](./samples/AspNetCoreIdentitySample/)
- [ASP.NET Core Blazor](./samples/BlazorSample/)
- [ASP.NET Core MVC](./samples/AspNetCoreMvcSample/)
- [OWIN](./samples/OwinSample/)

## FAQ

Before you ask questions, please check out these [issues](https://github.com/akunzai/GSS.Authentication.CAS/issues?q=is%3Aissue+label%3Aquestion), or read the [FAQ](https://github.com/akunzai/GSS.Authentication.CAS/wiki/FAQ) first.
