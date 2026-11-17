#!/usr/bin/env bash
# Komplettes Deployment von DartMetrics in Docker (z. B. Raspberry Pi): Prüfungen, Build, Start, Health-Check.
# Für reine Code-Updates reicht deploy/update-containers.sh (mit Datenbank-Backup).
# Gilt für die Instanz dieses Ordners (COMPOSE_PROJECT_NAME in der .env); andere laufende Instanzen werden gestoppt.
#
#   ./deploy/deploy.sh                 Entwicklung (lädt docker-compose.override.yml)
#   ./deploy/deploy.sh --production    nur docker-compose.yml: keine offenen DB/API-Ports, kein Mailpit
#   ./deploy/deploy.sh --no-cache      Images ohne Build-Cache
set -euo pipefail

PRODUCTION=0; NO_CACHE=""; TIMEOUT=180
for arg in "$@"; do
  case "$arg" in
    --production) PRODUCTION=1 ;;
    --no-cache) NO_CACHE="--no-cache" ;;
    *) echo "Unbekannte Option: $arg" >&2; exit 2 ;;
  esac
done

cd "$(dirname "$0")/.."
source deploy/instance.sh
step() { echo; echo "==> $1"; }
fail() { echo; echo "$1" >&2; exit 1; }
env_value() { grep -E "^$1=" .env 2>/dev/null | tail -n1 | cut -d= -f2- | sed 's/^"//; s/"$//'; }

[ "$PRODUCTION" -eq 1 ] && export COMPOSE_FILE=docker-compose.yml

# ---------- 1. Voraussetzungen ----------
step "Voraussetzungen prüfen"
docker info >/dev/null 2>&1 || fail "Docker läuft nicht."

if [ ! -f .env ]; then
  cp .env.example .env
  fail ".env fehlte und wurde aus .env.example angelegt. Bitte die Platzhalter (change-me, LEGAL_*) ersetzen und erneut starten."
fi

missing=""
for key in MYSQL_ROOT_PASSWORD MYSQL_PASSWORD JWT_KEY ADMIN_PASSWORD CERT_PASSWORD; do
  value="$(env_value "$key")"
  case "$value" in ""|change-me*) missing="$missing $key" ;; esac
done
[ -z "$missing" ] || fail "In der .env fehlen oder sind noch Platzhalter:$missing"
jwt_key="$(env_value JWT_KEY)"
[ "${#jwt_key}" -ge 32 ] || fail "JWT_KEY muss mindestens 32 Zeichen lang sein."
case "$(env_value LEGAL_NAME)" in ""|"Max Mustermann") echo "    Hinweis: LEGAL_* (Impressum/Datenschutz) sind noch nicht gesetzt." ;; esac
echo "    .env ok"

if [ ! -f certs/dartmetrics.pfx ]; then
  step "Selbstsigniertes HTTPS-Zertifikat erzeugen (certs/dartmetrics.pfx, 2 Jahre)"
  command -v openssl >/dev/null || fail "openssl fehlt (sudo apt install openssl)."
  mkdir -p certs
  tmp="$(mktemp -d)"
  openssl req -x509 -newkey rsa:2048 -nodes -days 730 -subj "/CN=localhost" \
    -addext "subjectAltName=DNS:localhost,DNS:$(hostname),IP:127.0.0.1" \
    -keyout "$tmp/key.pem" -out "$tmp/cert.pem" >/dev/null 2>&1
  openssl pkcs12 -export -out certs/dartmetrics.pfx -inkey "$tmp/key.pem" -in "$tmp/cert.pem" \
    -passout "pass:$(env_value CERT_PASSWORD)"
  rm -rf "$tmp"
  echo "    Hinweis: selbstsigniert - der Browser warnt einmalig. Für öffentlichen Betrieb ein echtes Zertifikat (Reverse Proxy/Tunnel) nutzen."
fi
echo "    Zertifikat ok"

if ! ls Web.App/Web.App/wwwroot/fonts/*.woff2 >/dev/null 2>&1; then
  step "Schriften laden"
  ./deploy/download-fonts.sh
fi
echo "    Schriften ok"

INSTANCE="$(instance_name)"
echo "    Instanz: $INSTANCE"

# ---------- 2. Build ----------
step "Images bauen (api, web)"
docker compose build --pull $NO_CACHE api web

# ---------- 3. Start ----------
step "Container starten"
stop_other_instances "$INSTANCE"
docker compose up -d --remove-orphans

step "Warten auf API und Web-App (max. ${TIMEOUT} s)"
deadline=$(( $(date +%s) + TIMEOUT ))
while :; do
  sleep 3
  api=$(service_state api)
  web=$(service_state web)
  echo "    api: $api, web: $web"
  [ "$api" = "healthy" ] && [ "$web" = "running" ] && break
  if [ "$(date +%s)" -ge "$deadline" ]; then
    docker compose logs --tail 40 api web
    fail "API oder Web-App laufen nicht. Logs oben prüfen."
  fi
done

# ---------- Fertig ----------
step "Status"
docker compose ps
echo
echo "Fertig (Instanz $INSTANCE)."
echo "  Web-App  https://localhost:7254"
if [ "$PRODUCTION" -eq 0 ]; then
  echo "  Swagger  http://localhost:8080/swagger"
  echo "  Mailpit  http://localhost:8025"
fi
echo "  Admin    $(env_value ADMIN_EMAIL || true) (Passwort aus der .env, beim ersten Login ändern)"
