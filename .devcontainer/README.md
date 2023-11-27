# Dev Containers for .NET and [Keycloak](https://www.keycloak.org)

## Requirements

- [Docker Engine](https://docs.docker.com/install/)
- [Docker Compose V2](https://docs.docker.com/compose/cli-command/)
- [mkcert](https://github.com/FiloSottile/mkcert)
- [Visual Studio Code](https://code.visualstudio.com/)
- Bash

## Getting Start

```sh
# set up TLS certs and hosts in Host
./init.sh auth.dev.local

# starting container
docker compose up -d

# run the sample app in Host
cd ../samples/AspNetCoreSample
dotnet run
```

## Admin URLs

- [Keycloak](https://auth.dev.local)

## Credentials

### Keycloak admin

- Username: `admin`
- Password: `admin`

### Keycloak user

- Username: `test`
- Password: `test`