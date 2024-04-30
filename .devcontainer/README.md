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

# export keycloak configurations
docker compose exec keycloak /opt/keycloak/bin/kc.sh export --dir /opt/keycloak/data/export/

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
