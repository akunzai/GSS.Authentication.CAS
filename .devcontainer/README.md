# Dev Containers for .NET and [Keycloak](https://www.keycloak.org)

## Requirements

- [Docker Engine](https://docs.docker.com/install/)
- [Docker Compose V2](https://docs.docker.com/compose/cli-command/)
- [mkcert](https://github.com/FiloSottile/mkcert)
- [Visual Studio Code](https://code.visualstudio.com/)
- Bash

## Getting Start

> The following instrstuctions are for macOS environment

```sh
# set up hostname
./scripts/setup-host.sh

# set up TLS certificates
./scripts/setup-tls.sh

# run the Dev Containers
docker compose up -d

# run the sample app in Host
cd ../samples/AspNetSample
dotnet run

# browser sample app and sign-in as demo user (test:test)
open https://localhost:5001
```
