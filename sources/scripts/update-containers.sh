#!/usr/bin/env bash
# Baut die DartMetrics-Container neu und startet sie aktualisiert.
# Die Daten (Volume mysql_data) bleiben erhalten.
#
#   ./scripts/update-containers.sh             nur die API neu bauen + neu starten
#   ./scripts/update-containers.sh --all       alle Dienste aktualisieren
#   ./scripts/update-containers.sh --all --pull  zusätzlich neue MySQL/Mailpit-Images holen
#   ./scripts/update-containers.sh --no-cache  API ohne Build-Cache bauen
set -euo pipefail

ALL=0; PULL=0; NO_CACHE=""; TIMEOUT=120
for arg in "$@"; do
  case "$arg" in
    --all) ALL=1 ;;
    --pull) PULL=1 ;;
    --no-cache) NO_CACHE="--no-cache" ;;
    *) echo "Unbekannte Option: $arg" >&2; exit 2 ;;
  esac
done

cd "$(dirname "$0")/.."

docker info >/dev/null 2>&1 || { echo "Docker läuft nicht." >&2; exit 1; }
[ -f .env ] || { echo ".env fehlt im Lösungsordner." >&2; exit 1; }

SERVICES=(); [ "$ALL" -eq 1 ] || SERVICES=(api)

step() { printf '\n==> %s\n' "$1"; }

if [ "$PULL" -eq 1 ]; then
  step "Neue Basis-Images holen"
  docker compose pull --ignore-buildable "${SERVICES[@]}"
fi

step "API-Image bauen"
docker compose build $NO_CACHE --pull api

step "Container ersetzen"
docker compose up -d --force-recreate --remove-orphans "${SERVICES[@]}"

step "Warte auf API (max. ${TIMEOUT} s)"
state=""; end=$((SECONDS + TIMEOUT))
while [ $SECONDS -lt $end ]; do
  sleep 3
  state=$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' dartmetrics-api 2>/dev/null || true)
  echo "    api: $state"
  case "$state" in healthy|exited|dead) break ;; esac
done

step "Alte, unbenutzte Images entfernen"
docker image prune -f

step "Status"
docker compose ps

step "Letzte API-Logs"
docker compose logs --tail 30 api

if [ "$state" != "healthy" ]; then
  echo "API ist nicht healthy (Status: $state). Logs oben prüfen." >&2
  exit 1
fi

echo
echo "Fertig."
echo "  API      http://localhost:8080/swagger"
echo "  Mailpit  http://localhost:8025"
