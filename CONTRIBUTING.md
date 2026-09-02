# Contributing to GSS.Authentication.CAS

Thank you for your interest in contributing! This guide covers how to report issues, propose features, and submit pull requests.

## Reporting Issues

- **Security vulnerabilities:** See [SECURITY.md](.github/SECURITY.md). Do **not** open public issues.
- **Bugs:** Use the [Bug Report](.github/ISSUE_TEMPLATE/bug_report.md) issue template.
- **Feature requests:** Use the [Feature Request](.github/ISSUE_TEMPLATE/feature_request.md) issue template.

## Development Setup

### Prerequisites

- [.NET SDK 8.0+](https://dot.net) (10.0 recommended)
- [mise](https://mise.jdx.dev) (pins Node + [aube](https://aube.jdx.dev), for the `e2e/` suite)
- [Docker](https://www.docker.com/) (for E2E tests with Keycloak)

### Clone & Build

```shell
git clone https://github.com/akunzai/GSS.Authentication.CAS.git
cd GSS.Authentication.CAS
dotnet build
```

### Running Tests

```shell
# Unit + integration tests (no external dependencies)
dotnet test

# With code coverage report
dotnet test --coverage --coverage-output-format cobertura
dotnet tool restore && dotnet tool run reportgenerator

# E2E tests (requires Keycloak — see .devcontainer/)
cd e2e
aube install
aube exec -- playwright install --with-deps chromium
aube exec -- playwright test
```

### Dev Container (recommended)

Open in VS Code with the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension. Keycloak is started automatically.

## Submitting a Pull Request

1. **Fork** the repository and create a branch from `main`:

   ```shell
   git checkout -b fix/your-bug-description
   ```

2. **Make your changes.** Follow the [code conventions](.editorconfig).

3. **Run tests** before pushing:

   ```shell
   dotnet build -c Release
   dotnet test
   ```

4. **Apply one primary label** to your PR (required; release-drafter puts each PR in the first matching group):

   | Label                      | When to use              |
   | -------------------------- | ------------------------ |
   | `breaking`                 | Breaking API change      |
   | `feature` / `enhancement`  | New functionality        |
   | `bug` / `fix`              | Bug fix                  |
   | `dependencies`             | Dependency updates       |
   | `github_actions`           | CI / workflow changes    |
   | `chore` / `refactor`       | Internal cleanup         |
   | `samples`                  | Sample apps only         |
   | `documentation`            | Docs only                |
   | `ignore-for-release`       | Not noteworthy for users |

5. **Open a PR** against `main`. Fill in the PR template.

## Code Conventions

- 4-space indentation for C# (enforced via `.editorconfig`)
- Nullable reference types enabled — avoid `!` suppression unless justified
- `TreatWarningsAsErrors` is on — zero warnings policy
- Private fields: `_camelCase`; constants/statics: `PascalCase`
- All public API should have XML documentation comments (`/// <summary>`)

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
