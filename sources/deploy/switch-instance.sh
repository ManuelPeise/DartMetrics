#!/usr/bin/env bash
# Startet die DartMetrics-Instanz dieses Ordners und stoppt alle anderen (ohne Neubau).
# Alle Instanzen nutzen dieselben Ports, es läuft immer nur eine. Daten bleiben erhalten.
# Für neuen Code: deploy/update-containers.sh (mit Datenbank-Backup).
#
#   ../DartMetrics-beta/sources/deploy/switch-instance.sh   zur Beta wechseln
#   ./deploy/switch-instance.sh --production                nur docker-compose.yml
set -euo pipefail

TIMEOUT=180
for arg in "$@"; do
  case "$arg" in
    --production) export COMPOSE_FILE=docker-compose.yml ;;
    *) echo "Unbekannte Option: $arg" >&2; exit 2 ;;
  esac
done

cd "$(dirname "$0")/.."
source deploy/instance.sh

docker info >/dev/null 2>&1 || { echo "Docker läuft nicht." >&2; exit 1; }
[ -f .env ] || { echo ".env fehlt im Lösungsordner (zuerst deploy/deploy.sh)." >&2; exit 1; }

INSTANCE="$(instance_name)"
printf '\n==> Wechsel zur Instanz %s (%s)\n' "$INSTANCE" "$PWD"
stop_other_instances "$INSTANCE"

docker compose up -d --remove-orphans

printf '\n==> Warten auf API und Web-App (max. %s s)\n' "$TIMEOUT"
deadline=$(( $(date +%s) + TIMEOUT ))
while :; do
  sleep 3
  api=$(service_state api); web=$(service_state web)
  echo "    api: $api, web: $web"
  [ "$api" = "healthy" ] && [ "$web" = "running" ] && break
  if [ "$(date +%s)" -ge "$deadline" ]; then
    docker compose logs --tail 40 api web
    echo "API oder Web-App laufen nicht. Logs oben prüfen." >&2
    exit 1
  fi
done

docker compose ps
echo
echo "Instanz '$INSTANCE' läuft: https://localhost:7254"
