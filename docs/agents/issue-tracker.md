# Issue tracker: GitHub

**This file is English throughout**, sample blocks included, so it reads
one way to every model, whatever language the repo chose for its issues.

Issues and specs for this repo (`akunzai/GSS.Authentication.CAS`) live as GitHub issues. Use the `gh` CLI for all operations.

Write issue titles and descriptions in **English**, matching the Language rule in `AGENTS.md`.

## Conventions

- **Create an issue**: `gh issue create --title "..." --body "..."`. Use a heredoc for multi-line bodies.
- **Read an issue**: `gh issue view <number> --comments`, filtering comments by `jq` and also fetching labels.
- **List issues**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Comment on an issue**: `gh issue comment <number> --body "..."`
- **Apply / remove labels**: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`
- **Close**: `gh issue close <number> --comment "..."`

Infer the repo from `git remote -v`; `gh` does this automatically when run inside a clone.

## Description shape

1. Open with what a new engineer or a library consumer would observe: the symptom or the request, in plain language. Skip file paths and function names unless the reader cannot otherwise locate the issue.
2. Add a visual GitHub renders inline — a Mermaid `sequenceDiagram` for a ticket-validation or single-sign-out flow, a screenshot for a sample-app bug. Skip formats the description editor cannot render, such as a link to an external artifact or a raw HTML or SVG file. Upload it with the repeatable `--attach` flag (`gh issue create --attach './bug.png#The error state'`); alt text follows the path after `#`. Only when capture is genuinely impossible, leave `<!-- screenshot pending: <what it should show> -->` rather than omitting it silently.
3. Close with a collapsed technical section, so it does not push the human summary below the fold:

```markdown
<details>
<summary>Technical details</summary>

suspected cause, related code paths, repro commands, log excerpts

</details>
```

**No personally identifiable information in any attachment**; use test data, masking, or cropping. CAS tickets, service URLs, and Keycloak accounts count as that data — `docs/agents/verification.md` holds the capture rules.

## Spec issues

An issue an agent will implement from carries a different shape, because its reader is building rather than triaging. Acceptance criteria stay above the fold; only background goes into `<details>`.

```markdown
<one paragraph: the observable outcome>

## Acceptance criteria

- [ ] <checkable statement about observable behaviour>
- [ ] <one per criterion; a reviewer can tick these without reading code>

## Scope

- In: <paths or areas>
- Out: <what this issue deliberately does not change>

## Verification

<how to prove it works, per docs/agents/verification.md; say here when this
needs the Keycloak dev container or a Windows machine rather than the
default `dotnet test` gate>

<details>
<summary>Technical details</summary>

related code paths, prior art, log excerpts, open questions

</details>
```

Use the vocabulary the project already defines for its domain, so the issue, the tests, and the code name the same things — CAS terms such as service ticket, proxy-granting ticket, and single sign-out keep their protocol meaning.

An issue with unanswered open questions is not ready to implement. Say so in the issue rather than letting an agent guess.

## Labels

Read this repo's own labels with `gh label list --limit 100`; the CLI defaults to 30 and reports that page as the whole set, so a label past the first page reads as absent. Nothing here invents a vocabulary; when a label really is missing, that is a conversation with the maintainer, not a label to create.

- **Triage roles** — `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix` — are owned by `docs/agents/triage-labels.md`. Read that file, not this list.
- **Applied when it applies**: `bug`, `enhancement`, `documentation`, `security`, `samples`, `dependencies`. One kind label per issue.
- **Required on every issue**: none.

Pull request labels are a separate vocabulary, driven by release notes. `CONTRIBUTING.md` owns that table.

## Pull requests as a triage surface

**PRs as a request surface: no.** _(Set to `yes` if this repo treats external PRs as feature requests; `/triage` reads this flag.)_

When set to `yes`, PRs run through the same labels and states as issues, using the `gh pr` equivalents:

- **Read a PR**: `gh pr view <number> --comments` and `gh pr diff <number>` for the diff.
- **List external PRs for triage**: `gh pr list --state open --json number,title,body,labels,author,authorAssociation,comments` then keep only `authorAssociation` of `CONTRIBUTOR`, `FIRST_TIME_CONTRIBUTOR`, or `NONE` (drop `OWNER`/`MEMBER`/`COLLABORATOR`).
- **Comment / label / close**: `gh pr comment`, `gh pr edit --add-label`/`--remove-label`, `gh pr close`.

GitHub shares one number space across issues and PRs, so a bare `#42` may be either: resolve with `gh pr view 42` and fall back to `gh issue view 42`.

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a single issue with **child** issues as tickets.

- **Map**: a single issue labelled `wayfinder:map`, holding the Notes / Decisions-so-far / Fog body. `gh issue create --label wayfinder:map`.
- **Child ticket**: an issue linked to the map as a GitHub sub-issue (`gh api` on the sub-issues endpoint). Where sub-issues aren't enabled, add the child to a task list in the map body and put `Part of #<map>` at the top of the child body. Labels: `wayfinder:<type>` (`research`/`prototype`/`grilling`/`task`). Once claimed, the ticket is assigned to the driving dev.
- **Blocking**: GitHub's **native issue dependencies**, the canonical, UI-visible representation. Add an edge with `gh api --method POST repos/<owner>/<repo>/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`, where `<blocker-db-id>` is the blocker's numeric **database id** (`gh api repos/<owner>/<repo>/issues/<n> --jq .id`, _not_ the `#number` or `node_id`). GitHub reports `issue_dependencies_summary.blocked_by` (open blockers only, the live gate). Where dependencies aren't available, fall back to a `Blocked by: #<n>, #<n>` line at the top of the child body. A ticket is unblocked when every blocker is closed.
- **Frontier query**: list the map's open children (`gh issue list --state open`, scoped to the map's sub-issues / task list), drop any with an open blocker (`issue_dependencies_summary.blocked_by > 0`, or an open issue in the `Blocked by` line) or an assignee; first in map order wins.
- **Claim**: `gh issue edit <n> --add-assignee @me`, the session's first write.
- **Resolve**: `gh issue comment <n> --body "<answer>"`, then `gh issue close <n>`, then append a context pointer (gist + link) to the map's Decisions-so-far.
