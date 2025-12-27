#!/usr/bin/env bash

# https://learn.microsoft.com/aspnet/core/security/docker-compose-https#macos-or-linux
if [[ -f "/https/aspnetapp.pem" ]] && [[ -f "/https/aspnetapp.key" ]]; then
    cat >> /home/vscode/.bashrc <<EOF
export ASPNETCORE_Kestrel__Certificates__Default__Path="/https/aspnetapp.pem"
export ASPNETCORE_Kestrel__Certificates__Default__KeyPath="/https/aspnetapp.key"
EOF
else
	dotnet dev-certs https --trust
fi

# Install Playwright browsers for E2E testing
# Only install if the E2E test project exists
if [[ -f "/workspace/test/GSS.Authentication.CAS.E2E.Tests/GSS.Authentication.CAS.E2E.Tests.csproj" ]]; then
    echo "Installing Playwright browsers..."
    cd /workspace/test/GSS.Authentication.CAS.E2E.Tests || exit
    dotnet build -c Release
    pwsh bin/Release/net10.0/playwright.ps1 install chromium --with-deps 2>/dev/null || true
fi