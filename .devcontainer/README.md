# Dev Containers for .NET and [Keycloak](https://www.keycloak.org)

## Requirements

- [Docker Engine](https://docs.docker.com/install/)
- [Docker Compose V2](https://docs.docker.com/compose/cli-command/)
- [Visual Studio Code](https://code.visualstudio.com/)
- Bash

## Getting Start

```sh
# set up hosts in Host
echo "127.0.0.1 auth.dev.local" | sudo tee -a /etc/hosts

# starting container
docker compose up -d

# run the AspNetCoreSample
dotnet run --project ../samples/AspNetCoreSample/AspNetCoreSample.csproj

# build the OwinSample
msbuild ../samples/OwinSample/OwinSample.csproj -verbosity:minimal -restore
```

## URLs

- [Keycloak Master Admin Console](http://auth.dev.local:8080/admin/master/console)
- [Keycloak Demo Account Console](http://auth.dev.local:8080/realms/demo/account)

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

### [Enabling HTTPS in ASP.NET using your own dev certificate](https://learn.microsoft.com/aspnet/core/security/docker-compose-https)

```sh
dotnet dev-certs https --export-path "${HOME}${env:USERPROFILE}/.aspnet/https/aspnetapp.pem" --format Pem --no-password
```
