# CAS Authentication Middleware Guidelines

CAS 1.0/2.0/3.0 authentication middleware for ASP.NET Core and OWIN/Katana. NuGet: `GSS.Authentication.CAS.{Core,AspNetCore,Owin}`.

## Language

All repo-facing content — code comments, commit messages, PR/issue titles and bodies, docs — is written in English, regardless of the conversation language used to produce it.

## Toolchain

Pinned in `mise.toml`:
- **.NET**: `global.json` SDK 10.x builds all TFMs; net8.0 is runtime-only for tests/samples.
- **Node**: React sample and `e2e/` use [aube](https://aube.jdx.dev) (`aube ci` for frozen install).

## Pointers

- Solutions: `CAS.slnx` (main), `owin/Owin.sln` (Windows-only)
- Ticket validation: `src/GSS.Authentication.CAS.Core/Validation/IServiceTicketValidator.cs`
- Proxy tickets (PGT/PGTIOU, `/proxy`): `src/GSS.Authentication.CAS.Core/Proxy/`
- AspNetCore handler & SLO: `src/GSS.Authentication.CAS.AspNetCore/CasAuthenticationHandler.cs`, `DistributedCacheTicketStore.cs`, `CasSingleLogoutOptions.cs`
- Gold-standard tests:
  - `test/GSS.Authentication.CAS.Core.Tests/Cas20ServiceTicketValidationTests.cs`
  - `test/GSS.Authentication.CAS.AspNetCore.Tests/CasAuthenticationMiddlewareTests.cs`
- Conventions: Central Package Management (CPM) in `Directory.Packages.props` (never `Version=` in `.csproj`).
- User docs: `docs/configuration.md`, `docs/single-sign-out.md`, `docs/proxy-tickets.md`
- E2E tests (Playwright Test, Node/aube): `e2e/playwright.config.ts` (one project per sample app), `e2e/support/`
- Before running or reporting verification, read `docs/agents/verification.md` — the gate command, what proves it ran, and what cannot be verified locally.
- When opening a pull request, read `docs/agents/pull-request.md` — adds to `.github/PULL_REQUEST_TEMPLATE.md`, which stays authoritative on structure.
- When filing or triaging an issue, read `docs/agents/issue-tracker.md` (triage labels: `docs/agents/triage-labels.md`, domain docs layout: `docs/agents/domain.md`).
- Gotchas: `docs/agents/lessons-learned.md` (e.g. running OWIN tests locally via Mono).

## Constraints

> [!WARNING]
> **OWIN ecosystem is frozen** — `Microsoft.Owin.*` 4.2.3, `Sustainsys.Saml2.AspNetCore2` 2.11.0 final.

- E2E tests require Keycloak + CAS protocol extension (`.devcontainer/`).

## Prevent Recurrence

- **Candidate**: Name who hits this again, in which file, on what change. No such scenario, nothing to propose.
- **Promote**: Offer the first tier that reaches them and only that one, pending confirmation — enforce it (assert/type/test) with its size quoted, else a comment at that site, else an agent-facing doc (`docs/agents/<topic>.md`, else `docs/agents/lessons-learned.md`) with one backtick-path line under Pointers and one sentence on why the tiers above cannot hold it.
- **Prune**: When adding to a file, audit the rest of it in the same pass. Drop entries once stale (obsolete version, now enforced, duplicated, or a transcript) — not by a fixed count.

## Claude Code Compatibility

`CLAUDE.md` is a symbolic link pointing to `AGENTS.md`. Edit `AGENTS.md` directly.
