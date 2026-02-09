# Aspire Sample

This directory contains the .NET Aspire orchestration and integration samples for GSS.Authentication.CAS.

## Quick Start

```shell
# 1. Requirements: Ensure Docker Desktop (or any Docker-compatible container runtime) is running.
# 2. Run Aspire AppHost:
dotnet run --project aspire/AspireSample.AppHost/AspireSample.AppHost.csproj

# 3. Explore:
# - Open the Aspire Dashboard (URL will be shown in the terminal).
# - Log in to any sample project using the demo account in Keycloak.
```

## Projects

- **AspireSample.AppHost**: The orchestrator that launches Keycloak and all sample projects.
- **AspireSample.ServiceDefaults**: Shared telemetry, health checks, and service discovery configuration.
- **AspireSample**: A dedicated sample project demonstrating direct Aspire integration.
