#!/usr/bin/env bash

# https://learn.microsoft.com/aspnet/core/security/docker-compose-https#macos-or-linux
if [ -f "/https/aspnetapp.pem" ] && [ -f "/https/aspnetapp.key" ]; then
    cat >> /home/vscode/.bashrc <<EOF
export ASPNETCORE_Kestrel__Certificates__Default__Path="/https/aspnetapp.pem"
export ASPNETCORE_Kestrel__Certificates__Default__KeyPath="/https/aspnetapp.key"
EOF
else
	dotnet dev-certs https --trust
fi