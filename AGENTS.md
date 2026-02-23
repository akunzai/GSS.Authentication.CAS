# Agent Guidelines for CAS authentication middleware

This file provides guidance to AI agents when working with code in this repository.

## Principles

- This root **AGENTS.md** contains only essential baseline knowledge. Domain-specific or advanced topics should be placed in `AGENTS.md` files within relevant subdirectories (e.g., `src/AGENTS.md`, `test/AGENTS.md`, `samples/AGENTS.md`). For cross-cutting topics without a corresponding subdirectory, place documentation under `docs/`.
- When solving problems reveals valuable knowledge, record it back to the most relevant `AGENTS.md` or documentation file.
- **CLAUDE.md** is a symlink to this file — avoid modifying CLAUDE.md directly.
- All documentation and code should be written in English.

## Project Overview

CAS (Central Authentication Service) authentication middleware for OWIN and ASP.NET Core, implementing CAS protocol versions 1.0, 2.0, and 3.0.

Published NuGet packages:

- `GSS.Authentication.CAS.Core` — core protocol logic, ticket validators, principal/assertion models
- `GSS.Authentication.CAS.AspNetCore` — ASP.NET Core authentication handler and middleware
- `GSS.Authentication.CAS.Owin` — OWIN/Katana authentication handler and middleware

## Build & Test Commands

```shell
# Build (main solution)
dotnet build

# Run unit/integration tests (excludes E2E)
dotnet test --filter "FullyQualifiedName!~E2E"

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage" --filter "FullyQualifiedName!~E2E"

# Generate coverage report
dotnet tool restore && dotnet tool run reportgenerator

# Run a single test by name
dotnet test --filter "FullyQualifiedName~Cas20ServiceTicketValidatorTests.ValidateAsync"

# Build OWIN solution (Windows + MSBuild only)
cd owin && msbuild -noLogo -verbosity:minimal -restore

# React sample client (in samples/AspNetCoreReactSample/ClientApp)
pnpm lint && pnpm build
```

E2E tests require a running Keycloak instance with the CAS protocol extension. See `.devcontainer/` for the Docker Compose setup.

## Architecture

### Layer Structure

```
GSS.Authentication.CAS.Core          (netstandard2.0, netcoreapp3.1)
  ├── GSS.Authentication.CAS.AspNetCore  (netcoreapp3.1, net8.0)
  └── GSS.Authentication.CAS.Owin       (netstandard2.0, net462)
```

Both AspNetCore and Owin depend on Core.

### Core Concepts

- **`IServiceTicketValidator`** — abstract ticket validation via HTTP with concrete implementations per CAS version (`Cas10`, `Cas20`, `Cas30`)
- **`CasPrincipal` / `CasIdentity`** — extend `ClaimsPrincipal` / `ClaimsIdentity` wrapping a CAS `Assertion` (principal name + attributes)
- **`CasAuthenticationHandler`** (AspNetCore) — `RemoteAuthenticationHandler<CasAuthenticationOptions>` handling challenge, ticket validation, and single sign-out
- **`CasSingleLogoutMiddleware`** — processes back-channel SAML 2.0 `LogoutRequest` from CAS server
- **`DistributedCacheTicketStore`** — `ITicketStore` backed by `IDistributedCache` for session storage (needed for Single Logout)

### Solution Files

- `CAS.slnx` — main solution (src + test + samples)
- `owin/Owin.sln` — OWIN-specific solution (Windows only)

## Code Conventions

Enforced via `.editorconfig`:

- 4-space indentation for C# (2-space for XML/JSON/YAML/web files)
- File-scoped namespaces
- Private/internal fields: `_camelCase`
- Constants and statics: `PascalCase`
- Prefer `var`
- `TreatWarningsAsErrors` is enabled globally
- Nullable reference types enabled in all projects

## Package Management

Uses Central Package Management (CPM) — all NuGet versions are defined in `Directory.Packages.props` at the root. Do not add `Version` attributes in individual `.csproj` files.

## Release Process

1. Merge PRs to `main` — draft release auto-updated by release-drafter
2. Push a `v*` tag — triggers NuGet publish via `dotnet pack -o packages` and GitHub release
