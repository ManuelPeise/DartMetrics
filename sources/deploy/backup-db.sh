#!/usr/bin/env bash
# Tägliches Datenbank-Backup für den Dauerbetrieb (z. B. Raspberry Pi), mit Rotation.
#
#   ./deploy/backup-db.sh              Backup nach backups/, die letzten 14 bleiben
#   KEEP_BACKUPS=30 ./deploy/backup-db.sh
#
# Per Cron jeden Tag um 03:15 (crontab -e):
#   15 3 * * * /pfad/zu/sources/deploy/backup-db.sh >> /pfad/zu/sources/backups/backup.log 2>&1
#
# Wiederherstellen:
#   gunzip -c backups/<datei>.sql.gz | docker exec -i mysql sh -c 'exec mysql -uroot -p"$MYSQL_ROOT_PASSWORD" DartContextDb'
set -euo pipefail

KEEP_BACKUPS="${KEEP_BACKUPS:-14}"
cd "$(dirname "$0")/.."
mkdir -p backups

if [ "$(docker inspect --format '{{.State.Running}}' mysql 2>/dev/null || true)" != "true" ]; then
  echo "$(date -Is) MySQL-Container läuft nicht - kein Backup." >&2
  exit 1
fi

file="backups/DartContextDb-$(date +%Y%m%d-%H%M%S).sql.gz"

# Passwort bleibt im Container; --hex-blob für Profilbilder.
docker exec mysql sh -c 'mysqldump -uroot -p"$MYSQL_ROOT_PASSWORD" --single-transaction --routines --hex-blob DartContextDb' | gzip > "$file"
echo "$(date -Is) Backup: $file ($(du -h "$file" | cut -f1))"

# Rotation: nur die neuesten KEEP_BACKUPS behalten
ls -1t backups/DartContextDb-*.sql.gz 2>/dev/null | tail -n +"$((KEEP_BACKUPS + 1))" | xargs -r rm -f
