#!/usr/bin/env bash
# Beta-Instanz von DartMetrics parallel zur normalen Installation (Projekt "dartmetrics-beta").
# Eigene Container, Volumes (eigene DB) und Ports; Konfiguration aus .env.beta.
#   ./deploy/deploy-beta.sh             bauen und starten
#   ./deploy/deploy-beta.sh --no-cache  ohne Build-Cache
#   ./deploy/deploy-beta.sh --reset     Beta inkl. Datenbank neu aufsetzen
#   ./deploy/deploy-beta.sh --remove    Beta komplett entfernen
set -euo pipefail
cd "$(dirname "$0")/.."

ENV_FILE=.env.beta
COMPOSE=(docker compose --env-file "$ENV_FILE" -f docker-compose.yml -f docker-compose.beta.yml)
NO_CACHE=""; RESET=0; REMOVE=0; TIMEOUT=180
for arg in "$@"; do
  case "$arg" in
    --no-cache) NO_CACHE="--no-cache" ;;
    --reset) RESET=1 ;;
    --remove) REMOVE=1 ;;
    *) echo "Unbekannte Option: $arg"; exit 1 ;;
  esac
done

step() { printf '\n\033[36m==> %s\033[0m\n' "$1"; }
fail() { printf '\n\033[31m%s\033[0m\n' "$1"; exit 1; }
val() { grep -E "^$1=" "$ENV_FILE" | tail -1 | cut -d= -f2- | tr -d '"' || true; }

docker info >/dev/null 2>&1 || fail "Docker läuft nicht."

if [ "$REMOVE" = 1 ]; then
  step "Beta entfernen (Container, Volumes, Images)"
  "${COMPOSE[@]}" down -v --rmi all --remove-orphans
  exit 0
fi

step "Voraussetzungen prüfen"
if [ ! -f "$ENV_FILE" ]; then
  src=.env; [ -f .env ] || src=.env.example
  cp "$src" "$ENV_FILE"
  printf '\n# Beta-Instanz\nBETA_HTTPS_PORT=7354\nBETA_HTTP_PORT=5223\nWEB_APP_PORT=7354\n' >> "$ENV_FILE"
  fail ".env.beta wurde aus $src angelegt. Bitte prüfen (Passwörter, Ports, WEB_APP_HOST/WEB_APP_PORT) und erneut starten."
fi
for key in MYSQL_ROOT_PASSWORD MYSQL_PASSWORD JWT_KEY ADMIN_PASSWORD CERT_PASSWORD; do
  v="$(val $key)"; { [ -z "$v" ] || [[ "$v" == change-me* ]]; } && fail "In der .env.beta fehlt oder ist Platzhalter: $key"
done
[ "$(val JWT_KEY | wc -c)" -gt 32 ] || fail "JWT_KEY muss mindestens 32 Zeichen lang sein."
[ -f certs/dartmetrics.pfx ] || fail "HTTPS-Zertifikat fehlt: certs/dartmetrics.pfx"
HTTPS_PORT="$(val BETA_HTTPS_PORT)"; HTTPS_PORT="${HTTPS_PORT:-7354}"

if [ "$RESET" = 1 ]; then
  step "Beta zurücksetzen (Container und Datenbank löschen)"
  "${COMPOSE[@]}" down -v --remove-orphans
fi

step "Images bauen (api, web :beta)"
"${COMPOSE[@]}" build --pull $NO_CACHE api web

step "Container starten"
"${COMPOSE[@]}" up -d --remove-orphans

step "Warten auf API und Web-App (max. ${TIMEOUT} s)"
end=$((SECONDS + TIMEOUT))
while :; do
  sleep 3
  api="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' dartmetrics-beta-api 2>/dev/null || true)"
  web="$(docker inspect --format '{{.State.Status}}' dartmetrics-beta-web 2>/dev/null || true)"
  echo "    api: $api, web: $web"
  [ "$api" = healthy ] && [ "$web" = running ] && break
  [ $SECONDS -ge $end ] && { "${COMPOSE[@]}" logs --tail 40 api web; fail "API oder Web-App laufen nicht."; }
done

"${COMPOSE[@]}" ps
printf '\nBeta läuft: https://localhost:%s\n' "$HTTPS_PORT"
