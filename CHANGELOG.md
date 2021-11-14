# ChangeLog

All notable changes to this project will be documented in this file.

## 5.0.1 (2021-11-14)

- Fixed System.InvalidOperationException: A suitable constructor for type 'GSS.Authentication.CAS.AspNetCore.CasSingleLogoutMiddleware' could not be located

## 5.0.0 (2021-07-03)

- Removed obsolete constructor [#91](https://github.com/akunzai/GSS.Authentication.CAS/pull/91)
- Fixes nullable property handling
- Fixes namespace
- Renamed Single Sign-Out to Single Logout (SLO) [#92](https://github.com/akunzai/GSS.Authentication.CAS/pull/92)

## 4.1.2 (2021-05-26)

- Add System.Text.Encodings.Web 4.7.2 to fix [#77](https://github.com/akunzai/GSS.Authentication.CAS/issues/77)
- Bump System.Text.Json from 4.6.0 to 4.7.2
- Bump Microsoft.Owin.Security.Cookies from 3.1.0 to 4.1.1 to fix [#75](https://github.com/akunzai/GSS.Authentication.CAS/issues/75)
- Bump Microsoft.AspNetCore.Authentication.Cookies from 2.1.2 to 2.2.0 [#79](https://github.com/akunzai/GSS.Authentication.CAS/pull/79)

## 4.1.1 (2021-05-05)

- Fixed single sign-out with null content-type [#68](https://github.com/akunzai/GSS.Authentication.CAS/issues/68)

## 4.1.0 (2019-12-05)

- Simplify targeting and dependencies [#31](https://github.com/akunzai/GSS.Authentication.CAS/pull/31)
- Improve error handling [#33](https://github.com/akunzai/GSS.Authentication.CAS/pull/33)
- Use ICookieManager to Read/Write CSRF Tokens [#37](https://github.com/akunzai/GSS.Authentication.CAS/pull/37)
- Add remote failure event support [#38](https://github.com/akunzai/GSS.Authentication.CAS/pull/38)

## 4.0.0 (2019-10-30)

- Support .NET Core 3.0
- Replace Newtonsoft.Json by System.Text.Json
- Refactoring ServiceTicket Serialization
- Use nullable reference types
- Delete obsolete methods

## 3.0.0 (2019-02-04)

- Signing assembly with Strong Name
- Aligning assembly version numbers

## 2018-11-28

### GSS.Authentication.CAS.DistributedCache 2.2.1

- Prefer await over ContinueWith

## 2018-10-18

### GSS.Authentication.CAS.DistributedCache 2.2.0

- Add Custom CacheKeyFactory
- Remove default constructor
- Fix renewed cache never expired
- Fix naming

### GSS.Authentication.CAS.RuntimeCache 1.5.0

- Add Custom CacheKeyFactory
- Fix naming

## 2018-10-16

### GSS.Authentication.CAS.Core 2.3.1

- Fix reference condition

### GSS.Authentication.CAS.Core 2.3.0

- Upgrade target from netstandard1.3 to netstandard2.0
- Upgrade target from net45 to net461
- Upgrade Newtonsoft.Json to 11.0.2

### GSS.Authentication.CAS.Owin 2.3.0

- Upgrade target from net45 to net461

### GSS.Authentication.CAS.RuntimeCache 1.4.0

- Upgrade target from net45 to net461

## 2018-06-25

### GSS.Authentication.CAS.Core 2.2.0

- Enable SourceLink

### GSS.Authentication.CAS.AspNetCore 2.1.1

- Fix reference version

### GSS.Authentication.CAS.AspNetCore 2.0.1

- Fix SingleSignOut ReadFormAsync error (issue #6)

### GSS.Authentication.CAS.Owin 2.2.1

- Fix reference version

### GSS.Authentication.CAS.Owin 2.1.1

- Fix SingleSignOut ReadFormAsync error (issue #6)

### GSS.Authentication.CAS.DistributedCache 2.1.0

- Enable SourceLink

### GSS.Authentication.CAS.RuntimeCache 1.3.0

- Enable SourceLink

## 2018-06-22

### GSS.Authentication.CAS.Core 2.1.2

- Fix StringValues deserialization (issue #5)

## 2018-05-25

### GSS.Authentication.CAS.Core 2.1.1

- Prefer ClaimsIdentity.NameClaimType instead of ClaimTypes.NameIdentifier

## 2018-03-20

### GSS.Authentication.CAS.Core 2.1.0

- Change attribute value type of Assertion to StringValues
- Ignore to insert default claims (NameIdentifier) to CasIdentity

### GSS.Authentication.CAS.Core 2.0.1

- Downgrade to .NET Standard 1.3
- Upgrade package reference

## 2018-03-19

### GSS.Authentication.CAS.Owin 2.1.0

- Add ServiceUrlBase options to override service base URL

## 2017-09-15

### GSS.Authentication.CAS.Core 2.0.0

- Migrate to .NET Standard 2.0 
- Add CasDefaults.AuthenticationType constant 
- CasOptions.AuthenticationType default to CasDefaults.AuthenticationType 

### GSS.Authentication.CAS.(AspNetCore|Owin|DistributedCache) 2.0.0

- Migrate to ASP.NET Core 2.0

## 2017-05-30

### GSS.Authentication.CAS.(Core|Owin|DistributedCache|RuntimeCache) 1.2.0

- Migrate from xproj to new csproj and upgrade dependencies

### GSS.Authentication.CAS.AspNetCore 1.2.0

- Migrate from xproj to new csproj and upgrade dependencies
- Fix IndexOutOfRangeException of CasSingleSignOutMiddleware (issue #5)

## 2016-08-10

### GSS.Authentication.CAS.(AspNetCore|Owin|DistributedCache) 1.1.0

- Optimize Serialization of Claims

### GSS.Authentication.CAS.Owin 1.0.1

- use dotnet CLI + project.json to pack nuget package

## 2016-06-14

### GSS.Authentication.CAS.Core 1.1.0

- Optimize Serialization of Claims

### GSS.Authentication.CAS.Core 1.0.1

- Change target framework to .NETStandard1.1

### GSS.Authentication.CAS.(Core|AspNetCore|Owin|DistributedCache|RuntimeCache) 1.0.0

- Initial Release
