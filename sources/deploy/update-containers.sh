#!/usr/bin/env bash
# Baut die DartMetrics-Container neu und startet sie aktualisiert.
# Die Daten (Volume mysql_data) bleiben erhalten.
#
#   ./deploy/update-containers.sh             nur die API neu bauen + neu starten
#   ./deploy/update-containers.sh --all       alle Dienste aktualisieren
#   ./deploy/update-containers.sh --all --pull  zusätzlich neue MySQL/Mailpit-Images holen
#   ./deploy/update-containers.sh --no-cache  API ohne Build-Cache bauen
#   ./deploy/update-containers.sh --test      vorher dotnet test ausführen (Abbruch bei Fehlern)
#   ./deploy/update-containers.sh --skip-backup  ohne Datenbank-Backup
#
# Vor dem Ersetzen wird die Datenbank nach backups/ gesichert (die API wendet Migrationen beim Start an).
# Es bleiben die letzten $KEEP_BACKUPS Sicherungen. Wiederherstellen:
#   docker exec -i mysql sh -c 'exec mysql -uroot -p"$MYSQL_ROOT_PASSWORD" DartContextDb' < backups/<datei>.sql
set -euo pipefail

ALL=0; PULL=0; NO_CACHE=""; TIMEOUT=120; RUN_TESTS=0; BACKUP=1; KEEP_BACKUPS=10
for arg in "$@"; do
  case "$arg" in
    --all) ALL=1 ;;
    --pull) PULL=1 ;;
    --no-cache) NO_CACHE="--no-cache" ;;
    --test) RUN_TESTS=1 ;;
    --skip-backup) BACKUP=0 ;;
    *) echo "Unbekannte Option: $arg" >&2; exit 2 ;;
  esac
done

cd "$(dirname "$0")/.."

docker info >/dev/null 2>&1 || { echo "Docker läuft nicht." >&2; exit 1; }
[ -f .env ] || { echo ".env fehlt im Lösungsordner." >&2; exit 1; }

SERVICES=(); [ "$ALL" -eq 1 ] || SERVICES=(api web)

step() { printf '\n==> %s\n' "$1"; }

if [ "$RUN_TESTS" -eq 1 ]; then
  step "Tests ausführen"
  dotnet test DartMetrics.slnx --nologo
fi

if [ "$BACKUP" -eq 1 ]; then
  if [ "$(docker inspect --format '{{.State.Running}}' mysql 2>/dev/null || true)" = "true" ]; then
    step "Datenbank sichern"
    mkdir -p backups
    file="backups/DartContextDb-$(date +%Y%m%d-%H%M%S).sql"
    # Passwort bleibt im Container (Umgebungsvariable), --hex-blob für Profilbilder.
    docker exec mysql sh -c 'exec mysqldump -uroot -p"$MYSQL_ROOT_PASSWORD" --single-transaction --routines --hex-blob DartContextDb' > "$file" \
      || { rm -f "$file"; echo "Backup fehlgeschlagen - Abbruch (--skip-backup zum Überspringen)." >&2; exit 1; }
    echo "    $file ($(du -h "$file" | cut -f1))"
    ls -1t backups/DartContextDb-*.sql | tail -n +$((KEEP_BACKUPS + 1)) | xargs -r rm -f
  else
    echo "MySQL läuft nicht - kein Backup (erster Start?)."
  fi
fi

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

step "Angewendete Migrationen (dieser Start)"
docker compose logs api 2>/dev/null | grep -E "Applying migration" || echo "    keine neuen"

if [ "$state" != "healthy" ]; then
  echo "API ist nicht healthy (Status: $state). Logs oben prüfen." >&2
  exit 1
fi

echo
echo "Fertig."
echo "  API      http://localhost:8080/swagger"
echo "  Mailpit  http://localhost:8025"
