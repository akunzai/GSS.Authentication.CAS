#!/usr/bin/env bash

[ -d "$HOME/.dotnet/tools" ] && export PATH="$PATH:$HOME/.dotnet/tools"

# https://learn.microsoft.com/aspnet/core/security/docker-compose-https#macos-or-linux
if [ -e "${HOME}/.aspnet/https/aspnetapp.pem" ] && [ -e "${HOME}/.aspnet/https/aspnetapp.key" ]; then
    export ASPNETCORE_Kestrel__Certificates__Default__Path="${HOME}/.aspnet/https/aspnetapp.pem"
    export ASPNETCORE_Kestrel__Certificates__Default__KeyPath="${HOME}/.aspnet/https/aspnetapp.key"
fi

host host.docker.internal >/dev/null 2>&1
if [ "$?" -eq "0" ]; then
    # override URLs in Dev Containers
    export ConnectionStrings__Redis="host.docker.internal"
	export CAS__ServerUrlBase="http://host.docker.internal:8080/realms/demo/protocol/cas"
    export OIDC__Authority="http://host.docker.internal:8080/realms/demo"
elif [ -n "$CODESPACE_NAME" ]; then
    # override URLs in GitHub Codespaces
    export CAS__ServerUrlBase="https://${CODESPACE_NAME}-8080.app.github.dev/realms/demo/protocol/cas"
    export OIDC__Authority="https://${CODESPACE_NAME}-8080.app.github.dev/realms/demo"
fi