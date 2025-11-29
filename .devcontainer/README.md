# Dev Containers for .NET and [Keycloak](https://www.keycloak.org)

## Requirements

- [Docker Engine](https://docs.docker.com/install/)
- [Docker Compose](https://docs.docker.com/compose/)
- [mkcert](https://github.com/FiloSottile/mkcert)
- [Visual Studio Code](https://code.visualstudio.com/)
- Bash

## Getting Start

```sh
# set up TLS certs in Host
mkdir -p .secrets
mkcert -cert-file .secrets/cert.pem -key-file .secrets/key.pem 'auth.dev.local'

# set up hosts in Host
echo "127.0.0.1 auth.dev.local" | sudo tee -a /etc/hosts

# starting container
docker compose up -d

# run the AspNetCoreSample
dotnet run --project ../samples/AspNetCoreSample/AspNetCoreSample.csproj
```

## URLs

- [Keycloak Master Admin Console](https://auth.dev.local:8443/admin/master/console)
- [Keycloak Demo Account Console](https://auth.dev.local:8443/realms/demo/account)

## Credentials

### Keycloak admin in Master realm

- Username: `admin`
- Password: `admin`

### Keycloak user in Demo realm

- Username: `test`
- Password: `test`

## E2E Testing with Playwright

By default, automated video recording and tracing are disabled. To enable them, set the following environment variables before running the tests:

```bash
# Enable Video Recording
export PLAYWRIGHT_VIDEO=true

# Enable Trace Recording
export PLAYWRIGHT_TRACE=true

# Run tests
dotnet test
```

The recordings will be saved in the `test/GSS.Authentication.CAS.E2E.Tests/bin/Debug/net8.0/recordings/` directory.

- **Videos**: Play `.webm` files with a modern browser or VLC.
- **Trace**: Upload the `.zip` file to [Playwright Trace Viewer](https://trace.playwright.dev/) for a full step-by-step playback with screenshots and console logs.
