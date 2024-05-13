#!/usr/bin/env bash

if [ -n "$CODESPACE_NAME" ]; then
	dotnet dev-certs https --trust
fi

cd $(dirname "$0")

docker compose up -d

cd -
sudo apt-get update
sudo apt-get install --no-install-recommends -yqq dnsutils