# GSS.Authentication.CAS

CAS Authentication Middleware for OWIN & ASP.NET Core

[![Build status](https://ci.appveyor.com/api/projects/status/uk7kwjvo1e6yl33m?svg=true)](https://ci.appveyor.com/project/akunzai/gss-authentication-cas)

## NuGet Packages

- [GSS.Authentication.CAS.Core ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.Core.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.Core/)
- [GSS.Authentication.CAS.Owin ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.Owin.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.Owin/)
- [GSS.Authentication.CAS.AspNetCore ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.AspNetCore.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.AspNetCore/)
- [GSS.Authentication.CAS.DistributedCache ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.DistributedCache.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.DistributedCache/)
- [GSS.Authentication.CAS.RuntimeCache ![NuGet version](https://img.shields.io/nuget/v/GSS.Authentication.CAS.RuntimeCache.svg?style=flat-square)](https://www.nuget.org/packages/GSS.Authentication.CAS.RuntimeCache/)

## Installation

OWIN

```shell
# Package Manager
Install-Package GSS.Authentication.CAS.Owin
Install-Package GSS.Authentication.CAS.Core

# .NET CLI
dotnet add package GSS.Authentication.CAS.Owin
dotnet add package GSS.Authentication.CAS.Core
```

ASP.NET Core 2.x

```shell
# Package Manager
Install-Package GSS.Authentication.CAS.AspNetCore
Install-Package GSS.Authentication.CAS.Core

# .NET CLI
dotnet add package GSS.Authentication.CAS.AspNetCore
dotnet add package GSS.Authentication.CAS.Core
```

## Usage

Currently, CAS protocol from 1.0 to 3.0 was supported.
See [samples](./samples/) for samples, such as:

- [ASP.NET Core app](./samples/AspNetCoreSample/)
- [ASP.NET Core app with Single-Sign-Out](./samples/AspNetCoreSingleSignOutSample/)
- [ASP.NET Core Identity app](./samples/AspNetCoreIdentitySample/)
- [ASP.NET MVC app](./samples/AspNetMvcSample/)
- [OWIN app](./samples/OwinSample/)
- [OWIN app with Single-Sign-Out](./samples/OwinSingleSignOutSample/)