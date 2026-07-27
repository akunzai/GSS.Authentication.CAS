# CAS Authentication Middleware — Agent Guidelines

CAS 1.0/2.0/3.0 auth middleware for ASP.NET Core and OWIN/Katana. NuGet: `GSS.Authentication.CAS.{Core,AspNetCore,Owin}`.

Root file = baseline only. Nested `AGENTS.md` under `src/` / `test/` / `samples/` for domain depth; cross-cutting SOPs under `docs/`. Docs & code in English. Propose writeback of non-obvious gotchas (context-tagged).

## Quick Commands

```shell
dotnet build
dotnet test --filter "FullyQualifiedName!~E2E"
dotnet test --collect:"XPlat Code Coverage" --filter "FullyQualifiedName!~E2E"
dotnet tool restore && dotnet tool run reportgenerator
dotnet test --filter "FullyQualifiedName~Cas20ServiceTicketValidatorTests.ValidateAsync"
cd owin && msbuild -noLogo -verbosity:minimal -restore   # Windows + MSBuild only
# samples/AspNetCoreReactSample/ClientApp
aube lint && aube build
```

E2E needs Keycloak + CAS protocol extension — see `@.devcontainer/`.

## Toolchain

Pinned in `@mise.toml` (`mise install`):

- **.NET** — `@global.json` SDK 10.x builds all TFMs; net8.0 is **runtime-only** (run tests/samples). net462/net48 (OWIN) = Windows + MSBuild (out of mise).
- **Node + aube** — React sample uses [aube](https://aube.jdx.dev) (not pnpm); mutates existing `pnpm-lock.yaml`. Frozen install: `aube ci`.

## Architecture

```
Core (netstandard2.0, netcoreapp3.1)
  ├── AspNetCore (netcoreapp3.1, net8.0, net10.0)
  └── Owin (netstandard2.0, net462)
```

- Solutions: `CAS.slnx` (main); `owin/Owin.sln` (Windows-only).
- Ticket validation → principal: `@src/GSS.Authentication.CAS.Core/Validation/IServiceTicketValidator.cs`, `@src/GSS.Authentication.CAS.Core/Security/`
- AspNetCore handler + SLO: `@src/GSS.Authentication.CAS.AspNetCore/CasAuthenticationHandler.cs`, `CasSingleLogoutMiddleware.cs`, `DistributedCacheTicketStore.cs` (SLO needs `ITicketStore` + `IDistributedCache`)
- Gold tests: `@test/GSS.Authentication.CAS.Core.Tests/Cas20ServiceTicketValidationTests.cs`, `@test/GSS.Authentication.CAS.AspNetCore.Tests/CasAuthenticationMiddlewareTests.cs`

## Conventions (SSOT)

- Style: `@.editorconfig` — do not restate here.
- Build flags: `@Directory.Build.props` (`TreatWarningsAsErrors`), `@src/Directory.Build.props` (nullable).
- **CPM**: versions only in `@Directory.Packages.props` — never `Version=` in `.csproj`. Legacy TFMs (`netstandard2.0` / `net462`) pin `Microsoft.Extensions.*` 8.0.x; modern TFMs use 10.x (conditional groups).

## Constraints (non-derivable)

> [!WARNING]
> **OWIN ecosystem is frozen** — `Microsoft.Owin.*` 4.2.3, `Sustainsys.Saml2.AspNetCore2` 2.11.0 final. No upstream fixes expected.

- Release: merge → `main` (release-drafter draft); push `v*` tag → `dotnet pack -o packages` + GitHub release.

## Claude Code Compatibility

> [!NOTE]
> `CLAUDE.md` is a symlink to `AGENTS.md`. Update this file only; do not edit or delete the symlink independently.
