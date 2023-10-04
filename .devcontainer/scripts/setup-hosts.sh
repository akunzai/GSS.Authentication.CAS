#!/usr/bin/env bash

set -euo pipefail

declare -a hosts=("auth.dev.local")

for host in "${hosts[@]}"; do
	if ! grep -q "${host}" /etc/hosts; then
		echo "127.0.0.1 ${host}" | sudo tee -a /etc/hosts
	fi
done