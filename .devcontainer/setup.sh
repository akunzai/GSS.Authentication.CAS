#!/usr/bin/env bash

if [ -n "$CODESPACE_NAME" ]; then
	dotnet dev-certs https --trust
fi

cd $(dirname "$0")

docker compose up -d

cd -