# Pull requests

**This file is English throughout**, sample blocks included, whatever
language the repo chose for its requests.

Write PR titles, descriptions, and comments in **English**, matching the
Language rule in `AGENTS.md`. **Git commit messages are English**,
imperative, subject under 72 characters — they live in history and get
searched by tooling.

This repo's `.github/PULL_REQUEST_TEMPLATE.md` is authoritative on
structure: Description, Type of Change, Checklist. What follows only adds
what it does not say.

## Preparing

- Work on a feature branch. Never prepare a request from `main`.
- Use a concise descriptive title with no Conventional Commit prefix,
  because one request may carry more than one kind of change. Release
  notes are grouped by label, not by title.
- Apply exactly one primary label. `.github/release-drafter.yml` puts each
  PR in the first matching group, and `label-check.yml` fails the request
  without one. The table in `CONTRIBUTING.md` says which label to pick.
- Link the issue in the Description with a closing keyword (`Fixes #123`).
- **Do not open a request, draft included, without the developer asking.**

## Description shape

1. A plain-language opening: what changed and why, as a reviewer who did
   not write it would need it.
2. A visual GitHub renders inline, chosen by what changed:

   | Change | Visual |
   | --- | --- |
   | CAS ticket flow, redirect, or single sign-out sequence | Mermaid `sequenceDiagram` |
   | Handler or middleware state transition | Mermaid `flowchart` / `stateDiagram` |
   | Sample app appearance | Before/after screenshots |
   | Library-only change | None; test output instead |

   Most changes here are library-only and need no diagram. Pair before and
   after. At most one diagram unless it is such a pair.

   Upload the file with the repeatable `--attach` flag —
   `gh pr create --attach './after.png#After'`. Alt text follows the path
   after `#`, and a path the body already references as
   `![alt](./after.png)` is rewritten to point at the uploaded asset. Only
   when capture is genuinely impossible, leave a named placeholder comment.
3. A collapsed technical trailer holding affected paths, implementation
   notes, verification commands, and log excerpts.

**No personally identifiable information in any attachment**, whatever
you end up attaching. `verification.md`'s capture rules say what that
means here.

## Tests land with the behaviour

- **Product logic**: `src/GSS.Authentication.CAS.Core/`,
  `src/GSS.Authentication.CAS.AspNetCore/`,
  `src/GSS.Authentication.CAS.Owin/`. A change here lands with its tests in
  the same request.
- **Public API surface**: a change to a public type or member also updates
  the matching `PublicAPI.Unshipped.txt`, per target framework for
  `AspNetCore`. The approval test fails otherwise, and the diff is what a
  reviewer reads to judge whether the change is breaking.
- **Exempt**: `docs/`, `.github/`, `*.md`, `mise.toml`, and dependency
  bumps with no behaviour change. `samples/` and `e2e/` are exempt from
  unit tests; a behaviour change reaching a sample is covered by the
  Playwright suite instead.
- **Structurally untestable** code — options classes, DI extension methods,
  all-static factories — is declared in the description, naming what covers
  it instead.

No coverage threshold. The reviewer judges whether the new behaviour is
actually exercised.

## Review readiness

Nothing unverified enters review. Verify locally per `verification.md`,
then open the request with the evidence. This repo publishes NuGet
packages and has no deployed environment to verify against, so there is no
draft-then-deploy path.

State in the description which paths were verified and which were not,
with the reason. A change touching `samples/` or `e2e/` names which
Playwright projects were run. The OWIN `net48` tests are the one gate that
commonly cannot run on a given machine; `verification.md` says what to
write when they did not.
