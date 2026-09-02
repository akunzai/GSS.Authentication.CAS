# Lessons Learned

## Running OWIN tests locally on Apple Silicon (arm64) Mac via Mono

`owin/GSS.Authentication.CAS.Owin.Tests` targets `net48` and needs Mono to run outside Windows/MSBuild (`dotnet build` compiles it fine on macOS; `dotnet test` needs Mono to execute the resulting `.exe`).

- `dotnet test --filter "..."` does not work under Mono here: the xunit.v3 console runner ignores/misinterprets `--filter` and just prints its own `--help` instead of filtering. Run the full test project (or use the runner's own filter flags) instead of relying on `--filter`.
- On Apple Silicon (arm64) Mono 6.12, `CasAuthenticationMiddlewareTests.SingleSignOut_ShouldRedirectToCasServer` fails even on an unmodified `main` — confirmed by running the same test from a `main` worktree. It's a pre-existing Mono-on-arm64-vs-.NET difference in how `Request.Scheme`/`Request.Host` resolve under `Microsoft.Owin.Testing.TestServer`, causing `BuildRedirectUriIfRelative` to treat an absolute URL as relative. Not a regression signal on that architecture; verify by diffing against `main` under the same Mono build before treating a failure here as real.
