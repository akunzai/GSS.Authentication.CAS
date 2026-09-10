# Verification

How an agent exercises a change in this repo before it reaches review.
Human setup narrative lives in `CONTRIBUTING.md`; this file holds only
what an agent needs.

## Starting the environment

```sh
dotnet test --ignore-exit-code 8
```

There is no stack to start. This is a library, so the gate command is the
verification.

`--ignore-exit-code 8` covers a partial run. Microsoft.Testing.Platform
reports exit code 8 for `Zero tests ran` under its strict zero-tests
policy, so any `--filter` naming a class that lives in one test project
only makes the other projects exit 8 and fails the whole run. The
unfiltered gate reaches every project and exits 0 with or without the
flag; it is carried on every command so one form works in both cases.
Exit code 8 never means a test failed — that is exit code 2.

The cost of that flag is that a suite which collapsed to nothing would
also pass, so `.github/workflows/build.yml` adds
`--minimum-expected-tests 300`, a solution-wide floor that fails with exit
code 9. `--ignore-exit-code 8` does not mask exit code 9. Local runs skip
the floor; the summary line is right there to read.

<!-- drift:forge github -->
<!-- drift:entrypoint-cmd dotnet test --ignore-exit-code 8 -->

It never prompts. A step needing a human aborts non-zero naming the
prerequisite — see Human prerequisites below.

**Proof it ran**: the summary line reports `failed: 0` and a non-zero
`total`. A run that reports `total: 0` proves nothing; the filter or the
build is wrong.

## Checks

| What | Command |
| --- | --- |
| The gate | `dotnet test --ignore-exit-code 8` |
| One test class while iterating | `dotnet test --filter "Cas20ServiceTicketValidationTests" --ignore-exit-code 8` |
| Release build, zero warnings (`TreatWarningsAsErrors`) | `dotnet build -c Release` |
| Coverage report | `dotnet test --coverage --coverage-output-format cobertura --ignore-exit-code 8` then `dotnet tool restore && dotnet tool run reportgenerator` |
| React sample client | `aube lint && aube build` in `samples/AspNetCoreReactSample/ClientApp` |
| OWIN targets (`net462`/`net48`) | `cd owin && msbuild -noLogo -verbosity:minimal -restore` — Windows + MSBuild only |
| Browser E2E | See Browser E2E below; the suite needs Keycloak and a sample app already listening |

A public API change also needs `PublicAPI.Unshipped.txt` updated, per
target framework for `AspNetCore`; the approval test in the gate fails
until it is.

## Browser E2E

Playwright has no `webServer` block, so nothing starts the stack for you.
One Playwright project per sample, each expecting its own port. Run one
project at a time, in this order.

```sh
# once per machine, and after a Keycloak image rebuild
cd .devcontainer && docker compose up -d keycloak redis
curl -sk -o /dev/null -w '%{http_code}\n' https://auth.dev.local:8443/realms/demo   # want 200

# once per clone
cd e2e && aube ci && aube exec -- playwright install chromium
cd samples/AspNetCoreReactSample/ClientApp && aube ci   # the react project only

# per project: start the sample, wait for it, then run that project alone
dotnet run --project samples/AspNetCoreSample --urls https://localhost:5001 &
cd e2e && aube exec -- playwright test --project=basic
```

`--with-deps` on `playwright install` is Linux-only; drop it elsewhere.

| Playwright project | Sample | URL |
| --- | --- | --- |
| `basic` | `samples/AspNetCoreSample` | `https://localhost:5001` |
| `mvc` | `samples/AspNetCoreMvcSample` | `https://localhost:5002` |
| `blazor` | `samples/BlazorSample` | `https://localhost:5003` |
| `identity` | `samples/AspNetCoreIdentitySample` | `https://localhost:5004` |
| `react` | `samples/AspNetCoreReactSample` | `https://localhost:5005` |

`.github/workflows/e2e-tests.yml` runs one matrix job per row and is the
reference when this drifts.

All five projects pass on macOS arm64 with Docker 29.4.0, so a failure
here is a signal, not an environment quirk. Where a machine lacks Docker,
mkcert, or the hosts entry, say the suite did not run rather than claiming
a pass.

Test credentials come from `.devcontainer/README.md`, and they are fixture
accounts in a throwaway realm. Real credentials never enter this suite.

## Human prerequisites

Run once, by a person. The commands above fail until they are done.

- [ ] Install the toolchain: `mise install` (.NET 10 SDK, the net8.0
      ASP.NET Core runtime, Node, aube).
- [ ] Install Docker and mkcert, for the Keycloak-backed E2E suite.
- [ ] Issue the TLS certificate Keycloak mounts:
      `cd .devcontainer && mkdir -p .secrets && mkcert -cert-file .secrets/cert.pem -key-file .secrets/key.pem auth.dev.local`.
- [ ] Map the Keycloak hostname:
      `echo "127.0.0.1 auth.dev.local" | sudo tee -a /etc/hosts`.
- [ ] Trust the ASP.NET Core dev certificate: `dotnet dev-certs https --trust`.
- [ ] For the OWIN targets outside Windows, install Mono. See
      `docs/agents/lessons-learned.md` first; `--filter` does not work
      under it, and one test fails on Apple Silicon on unmodified `main`.

The `wizard` skill can turn this list into an interactive script. Do not
have an agent run that script.

## Ports

Not applicable to the gate. The library has no listening service, so
several agents can run `dotnet test` in the same clone at once.

The dev container's Keycloak and the sample apps do listen, and their
ports are fixed. **Only one agent runs the E2E stack at a time.**

## Capturing evidence

- Test output: paste the summary block from the gate. It is the primary
  evidence for a library change.
- Recording: the `to-walkthrough-video` skill for a sample-app flow,
  `tcut` for a terminal session. Where neither is available, describe the
  steps and attach a screenshot.
- Screenshots: Playwright's own capture from `e2e/`, or the OS screenshot
  tool for a sample app.

**This document is where the capture rules live**, and
`docs/agents/pull-request.md` points here rather than restating them. A
capture taken on the developer's own machine carries their account's data,
username, and home paths as readily as a shared environment does. CAS
tickets, service URLs, and Keycloak accounts appearing in a log or a frame
count as that data. Assert on the frame, a marker, or fixture data, and
crop or mask what the tool happened to be showing.

For a change behind a mode switch or feature flag, confirm the far end
received the call. A healthy container and a green build are not evidence
that an integration is wired up.

## Not verified

Say in the request which of these did not run, and why.

- `owin/GSS.Authentication.CAS.Owin.Tests` (`net48`): needs Windows with
  MSBuild, or Mono elsewhere. On Apple Silicon under Mono,
  `SingleSignOut_ShouldRedirectToCasServer` fails on unmodified `main` —
  see `docs/agents/lessons-learned.md` before reading a failure as a
  regression. Neither `mono` nor `msbuild` is on PATH here, so this is a
  missing dependency rather than a check nobody tried.

A gap you could have closed is not a gap. Run the check whose dependency
you have already seen running, and report a check you skipped as untried,
rather than recording it here as one this repo cannot run.

<!-- drift:file .devcontainer/devcontainer.json -->
<!-- drift:file .devcontainer/compose.yaml -->
<!-- drift:file e2e/playwright.config.ts -->
