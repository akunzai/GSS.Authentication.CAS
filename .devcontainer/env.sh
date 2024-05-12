#!/usr/bin/env bash

corepack enable

[ -d "$HOME/.dotnet/tools" ] && export PATH="$PATH:$HOME/.dotnet/tools"

# override URLs for GitHub Codespaces
if [ -n "$CODESPACE_NAME" ]; then
    export CAS__ServerUrlBase="https://${CODESPACE_NAME}-8080.app.github.dev/realms/demo/protocol/cas"
fi

if [ -e "${HOME}/.aspnet/https/aspnetapp.pem" ] && [ -e "${HOME}/.aspnet/https/aspnetapp.key" ]; then
    export ASPNETCORE_Kestrel__Certificates__Default__Path="${HOME}/.aspnet/https/aspnetapp.pem"
    export ASPNETCORE_Kestrel__Certificates__Default__KeyPath="${HOME}/.aspnet/https/aspnetapp.key"
fi