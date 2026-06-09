# Pull Request

## Description

<!-- What does this PR do? Link to related issue if applicable (e.g. Fixes #123) -->

## Type of Change

- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change (requires major version bump)
- [ ] Refactoring / maintenance
- [ ] Documentation update
- [ ] Dependency update
- [ ] CI/CD change

## Checklist

- [ ] I have applied an appropriate **PR label** (required for release notes)
- [ ] `dotnet build -c Release` passes with no warnings
- [ ] `dotnet test --filter "FullyQualifiedName!~E2E"` passes
- [ ] Added or updated tests for behavior changes
- [ ] Public API changes include XML documentation comments
