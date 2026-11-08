# Dev Containers for .NET and [Keycloak](https://www.keycloak.org)

## Requirements

- [Podman](https://podman.io/) (recommended) or [Docker Engine](https://docs.docker.com/install/)
- [Podman Compose](https://github.com/containers/podman-compose) or [Docker Compose](https://docs.docker.com/compose/)
- [mkcert](https://github.com/FiloSottile/mkcert)
- DevContainer-compatible IDE (e.g. Visual Studio Code, JetBrains IDEs, Zed) or Dev Container CLI
- Bash

## Getting Start

```sh
# set up TLS certs in Host
mkdir -p .secrets
mkcert -cert-file .secrets/cert.pem -key-file .secrets/key.pem 'auth.dev.local'

# set up hosts in Host
echo "127.0.0.1 auth.dev.local" | sudo tee -a /etc/hosts

# starting container
podman compose up -d

# run the AspNetCoreSample
dotnet run --project ../samples/AspNetCoreSample/AspNetCoreSample.csproj
```

> [!TIP]
> You can override local toolchain environment variables in `mise.local.toml` (git-ignored) to customize the compose engine:
>
> ```toml
> [env]
> CONTAINER_COMPOSE = "podman compose"
> ```

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
