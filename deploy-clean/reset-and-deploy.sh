#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

if ! command -v docker >/dev/null 2>&1; then
  echo "ERROR: docker is not installed or not on PATH."
  exit 1
fi

if ! docker compose version >/dev/null 2>&1; then
  echo "ERROR: Docker Compose v2 is required (the 'docker compose' command)."
  exit 1
fi

if [ ! -f ".env" ]; then
  echo "ERROR: .env not found. Copy .env.example to .env and fill in values."
  exit 1
fi

# Read WEBUI_PORT from .env for printing the final URL.
# This parsing is intentionally simple and expects lines like: KEY=VALUE
WEBUI_PORT_VALUE="$(grep -E '^WEBUI_PORT=' .env | tail -n 1 | cut -d '=' -f2 | tr -d ' \r')"
if [ -z "$WEBUI_PORT_VALUE" ]; then
  WEBUI_PORT_VALUE="7159"
fi

echo "Stopping containers..."
docker compose --env-file .env down --remove-orphans || true

echo "Removing containers (if any remain)..."
docker rm -f onemanvan-webui onemanvan-db >/dev/null 2>&1 || true

echo "Removing volumes (FULL RESET: deletes DB data)..."
docker volume rm -f onemanvan-sqldata >/dev/null 2>&1 || true

echo "Removing network (if exists)..."
docker network rm onemanvan-network >/dev/null 2>&1 || true

echo "Building and starting fresh..."
docker compose --env-file .env up -d --build

echo "Done."
echo "Web UI: http://192.168.100.107:${WEBUI_PORT_VALUE}"
