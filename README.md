# GSS.Authentication.CAS

[![Build Status][ci-badge]][ci] [![Code Coverage][codecov-badge]][codecov]

[ci]: https://github.com/akunzai/GSS.Authentication.CAS/actions?query=workflow%3ACI
[ci-badge]: https://github.com/akunzai/GSS.Authentication.CAS/workflows/CI/badge.svg
[codecov]: https://codecov.io/gh/akunzai/GSS.Authentication.CAS
[codecov-badge]: https://codecov.io/gh/akunzai/GSS.Authentication.CAS/branch/main/graph/badge.svg?token=JGG7Y07SR0

CAS Authentication Middleware for OWIN & ASP.NET Core

## NuGet Packages

- [GSS.Authentication.CAS.Core ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.Core.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.Core/)
- [GSS.Authentication.CAS.Owin ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.Owin.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.Owin/)
- [GSS.Authentication.CAS.AspNetCore ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.AspNetCore.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.AspNetCore/)
- [GSS.Authentication.CAS.DistributedCache ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.DistributedCache.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.DistributedCache/)

## Installation

OWIN

```shell
# Package Manager
Install-Package GSS.Authentication.CAS.Owin

# .NET CLI
dotnet add package GSS.Authentication.CAS.Owin
```

ASP.NET Core

```shell
# Package Manager
Install-Package GSS.Authentication.CAS.AspNetCore

# .NET CLI
dotnet add package GSS.Authentication.CAS.AspNetCore
```

## Usage

Currently, CAS protocol from 1.0 to 3.0 was supported.
Check out these [samples](./samples/) to learn the basics and key features.

- [ASP.NET Core app](./samples/AspNetCoreSample/)
- [ASP.NET Core app with Single-Sign-Out](./samples/AspNetCoreSingleSignOutSample/)
- [ASP.NET Core Identity app](./samples/AspNetCoreIdentitySample/)
- [OWIN app](./samples/OwinSample/)
- [OWIN app with Single-Sign-Out](./samples/OwinSingleSignOutSample/)

## FAQ

Before you ask questions, please check out these [issues](https://github.com/akunzai/GSS.Authentication.CAS/issues?q=is%3Aissue+label%3Aquestion), or read the [FAQ](https://github.com/akunzai/GSS.Authentication.CAS/wiki/FAQ) first.
