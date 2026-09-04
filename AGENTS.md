# CAS Authentication Middleware Guidelines

CAS 1.0/2.0/3.0 authentication middleware for ASP.NET Core and OWIN/Katana. NuGet: `GSS.Authentication.CAS.{Core,AspNetCore,Owin}`.

## Language

All repo-facing content — code comments, commit messages, PR/issue titles and bodies, docs — MUST be written in English, regardless of the conversation language used to produce it.

## Commands

```shell
dotnet build
dotnet test --ignore-exit-code 8
dotnet test --coverage --coverage-output-format cobertura --ignore-exit-code 8
dotnet tool restore && dotnet tool run reportgenerator
dotnet test --filter "Cas20ServiceTicketValidationTests" --ignore-exit-code 8
cd owin && msbuild -noLogo -verbosity:minimal -restore   # Windows + MSBuild only
aube lint && aube build                                   # samples/AspNetCoreReactSample/ClientApp
cd e2e && aube install && aube exec -- playwright test    # requires Keycloak + samples running, see .devcontainer/
```

## Toolchain

Pinned in `@mise.toml`:
- **.NET**: `@global.json` SDK 10.x builds all TFMs; net8.0 is runtime-only for tests/samples.
- **Node**: React sample and `e2e/` use [aube](https://aube.jdx.dev) (`aube ci` for frozen install).

## Pointers

- Solutions: `CAS.slnx` (main), `owin/Owin.sln` (Windows-only)
- Ticket validation: `@src/GSS.Authentication.CAS.Core/Validation/IServiceTicketValidator.cs`
- Proxy tickets (PGT/PGTIOU, `/proxy`): `@src/GSS.Authentication.CAS.Core/Proxy/`
- AspNetCore handler & SLO: `@src/GSS.Authentication.CAS.AspNetCore/CasAuthenticationHandler.cs`, `DistributedCacheTicketStore.cs`, `CasSingleLogoutOptions.cs`
- Gold-standard tests:
  - `@test/GSS.Authentication.CAS.Core.Tests/Cas20ServiceTicketValidationTests.cs`
  - `@test/GSS.Authentication.CAS.AspNetCore.Tests/CasAuthenticationMiddlewareTests.cs`
- Conventions: Central Package Management (CPM) in `@Directory.Packages.props` (never `Version=` in `.csproj`).
- User docs: `@docs/configuration.md`, `@docs/single-sign-out.md`, `@docs/proxy-tickets.md`
- E2E tests (Playwright Test, Node/aube): `@e2e/playwright.config.ts` (one project per sample app), `@e2e/support/`
- Agent skills config: issues on GitHub (`@docs/agents/issue-tracker.md`), triage labels (`@docs/agents/triage-labels.md`), domain docs layout (`@docs/agents/domain.md`).
- Gotchas: `@docs/agents/lessons-learned.md` (e.g. running OWIN tests locally via Mono).

## Constraints

> [!WARNING]
> **OWIN ecosystem is frozen** — `Microsoft.Owin.*` 4.2.3, `Sustainsys.Saml2.AspNetCore2` 2.11.0 final.

- E2E tests require Keycloak + CAS protocol extension (`@.devcontainer/`).

## Self-Reflection

- **Candidate**: Distill a non-obvious gotcha into ≤ 2 context-tagged bullets. Propose it before writing.
- **Promote**: On confirmation, put it where whoever would break it must already pass — enforce it (assert/type/test) when the fix is in hand, else a comment at that site, else an agent-facing doc (`docs/agents/<topic>.md`, else `docs/agents/lessons-learned.md`) with one `@path` line under Pointers. Never both.
- **Prune**: Drop entries once stale (obsolete version, now enforced, duplicated, or a transcript) — not by a fixed count.

## Claude Code Compatibility

`CLAUDE.md` is a symbolic link pointing to `AGENTS.md`. Edit `AGENTS.md` directly.
