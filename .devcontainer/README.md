# Dev Containers for .NET and [Keycloak](https://www.keycloak.org)

## Requirements

- [Docker Engine](https://docs.docker.com/install/)
- [Docker Compose V2](https://docs.docker.com/compose/cli-command/)
- [Visual Studio Code](https://code.visualstudio.com/)
- Bash

## Getting Start

```sh
# starting container
docker compose up -d

# run the AspNetCoreSample in Host
dotnet run --project ../samples/AspNetCoreSample/AspNetCoreSample.csproj

# build the OwinSample
sh ./install_mono.sh
msbuild ../samples/OwinSample/OwinSample.csproj -verbosity:minimal -restore
```

## URLs

- [Keycloak Master Admin Console](http://localhost:8080/admin/master/console)
- [Keycloak Demo Account Console](http://localhost:8080/realms/demo/account)

## Credentials

### Keycloak admin in Master realm

- Username: `admin`
- Password: `admin`

### Keycloak user in Demo realm

- Username: `test`
- Password: `test`

## Troubleshooting

### [Exporting Keycloak](https://www.keycloak.org/server/importExport)

```sh
docker compose exec keycloak /opt/keycloak/bin/kc.sh export --dir /opt/keycloak/data/export/ --realm demo
```
