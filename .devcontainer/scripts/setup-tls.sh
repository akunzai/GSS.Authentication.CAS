#!/usr/bin/env bash

set -euo pipefail
CURRENTDIR=$(dirname "$0")

CERT_FILE="${CURRENTDIR}/../keycloak/tls/cert.pem"
KEY_FILE="${CURRENTDIR}/../keycloak/tls/key.pem"

if [ -e "${KEY_FILE}" ] && [ -e "${CERT_FILE}" ]; then
	echo "Certificate already exists"
	exit 0
fi

if [ -z "$(command -v mkcert)" ]; then
	echo "mkcert is not installed, try 'brew install mkcert'"
	exit 1
fi

mkcert -install
mkdir -vp $(dirname "$CERT_FILE")
mkcert -cert-file "$CERT_FILE" -key-file "$KEY_FILE" '*.dev.local'