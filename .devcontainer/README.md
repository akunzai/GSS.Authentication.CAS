# Dev Containers for .NET and [Keycloak](https://www.keycloak.org)

## Requirements

- [Docker Engine](https://docs.docker.com/install/)
- [Docker Compose V2](https://docs.docker.com/compose/cli-command/)
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

## Troubleshooting

### [Exporting Keycloak](https://www.keycloak.org/server/importExport)

```sh
docker compose exec keycloak /opt/keycloak/bin/kc.sh export --dir /opt/keycloak/data/export/ --realm demo
```

### [Enabling HTTPS in ASP.NET using your own dev certificate](https://learn.microsoft.com/aspnet/core/security/docker-compose-https)

```sh
dotnet dev-certs https --export-path "${HOME}${env:USERPROFILE}/.aspnet/https/aspnetapp.pem" --format Pem --no-password
```
