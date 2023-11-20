#!/usr/bin/env bash

set -euo pipefail

if [ $# -le 0 ] || [ "$1" == "--help" ] || [ "$1" == "-h" ]; then
	echo "Usage: $0 <host> [<host>...]"
	exit 1
fi

for host in "$@"; do
	if ! grep -q "${host}" /etc/hosts; then
		echo "127.0.0.1 ${host}" | sudo tee -a /etc/hosts
	fi
done

CURRENTDIR=$(dirname "$0")
CERT_FILE="${CURRENTDIR}/certs/cert.pem"
KEY_FILE="${CURRENTDIR}/certs/key.pem"

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
mkcert -cert-file "$CERT_FILE" -key-file "$KEY_FILE" $@