#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [[ ! -f .env ]]; then
  cp .env.example .env
fi

for _ in $(seq 1 30); do
  if docker info >/dev/null 2>&1; then
    docker compose up -d --build
    exit 0
  fi
  sleep 2
done

echo "Docker daemon is not available. Start it and run: docker compose up -d --build" >&2
exit 1
