#!/usr/bin/env bash
# Erzeugt, trainiert, prüft und aktiviert das ML-Zielwahl-Modell der DartBots - komplett in Docker.
# Schritte (Tools.BotTrainer im Container "bottrainer"): generate -> train -> compare -> activate.
# Aktiviert wird nur, wenn das Modell auf keiner Stufe signifikant schlechter ist als das Regelwerk.
# Voraussetzung: MySQL und API laufen (Migrationen angewendet), z. B. nach scripts/deploy.sh.
#
#   ./scripts/train-model.sh                       nur, wenn noch kein Modell aktiv ist
#   ./scripts/train-model.sh --force               neues Modell, auch wenn schon eins aktiv ist
#   ./scripts/train-model.sh --situations 300 --rollouts 30 --legs 500   schneller Probelauf
#   ./scripts/train-model.sh --activate-anyway     auch bei schlechterem Vergleich aktivieren
#
# Dauer mit Standardwerten: grob 10-30 Minuten auf einem PC, auf einem Raspberry Pi deutlich länger.
set -euo pipefail

SITUATIONS=1000; ROLLOUTS=50; LEGS=2000; SEED=1; FORCE=0; ACTIVATE_ANYWAY=0
while [ $# -gt 0 ]; do
  case "$1" in
    --situations) SITUATIONS="$2"; shift ;;
    --rollouts) ROLLOUTS="$2"; shift ;;
    --legs) LEGS="$2"; shift ;;
    --seed) SEED="$2"; shift ;;
    --force) FORCE=1 ;;
    --activate-anyway) ACTIVATE_ANYWAY=1 ;;
    *) echo "Unbekannte Option: $1" >&2; exit 2 ;;
  esac
  shift
done

cd "$(dirname "$0")/.."
LOG="$(mktemp)"; trap 'rm -f "$LOG"' EXIT

trainer() {
  echo; echo "==> bottrainer $*"
  set +e
  docker compose --profile tools run --rm bottrainer "$@" 2>&1 | tee "$LOG"
  local code=${PIPESTATUS[0]}
  set -e
  return "$code"
}

docker info >/dev/null 2>&1 || { echo "Docker läuft nicht." >&2; exit 1; }

active=$(docker exec mysql sh -c 'mysql -N -uroot -p"$MYSQL_ROOT_PASSWORD" DartContextDb -e "SELECT COUNT(*) FROM DartBotActiveModels" 2>/dev/null') \
  || { echo "Datenbank nicht erreichbar oder Tabellen fehlen. Läuft 'docker compose up -d'?" >&2; exit 1; }

if [ "$FORCE" -eq 0 ] && [ "${active:-0}" -gt 0 ]; then
  echo "Es ist bereits ein Zielwahl-Modell aktiv. Neu trainieren mit --force."
  exit 0
fi

export BOTTRAINER_COMMIT="$(git rev-parse --short HEAD 2>/dev/null || true)"

echo "==> Trainer-Image bauen"
docker compose --profile tools build bottrainer

trainer generate --situations "$SITUATIONS" --rollouts "$ROLLOUTS" --seed "$SEED"
DATASET_ID=$(grep -oE 'DartBotTrainingDatasets\.Id = [0-9]+' "$LOG" | grep -oE '[0-9]+$')
[ -n "$DATASET_ID" ] || { echo "Datensatz-Id nicht gefunden." >&2; exit 1; }

trainer train --dataset "$DATASET_ID" --seed "$SEED"
MODEL_ID=$(grep -oE 'DartBotModels: Id [0-9]+' "$LOG" | grep -oE '[0-9]+$')
[ -n "$MODEL_ID" ] || { echo "Modell-Id nicht gefunden." >&2; exit 1; }

set +e
trainer compare --model "$MODEL_ID" --legs "$LEGS" --seed "$SEED"
COMPARE=$?
set -e
if [ "$COMPARE" -eq 4 ] && [ "$ACTIVATE_ANYWAY" -eq 0 ]; then
  echo; echo "Modell $MODEL_ID ist auf mindestens einer Stufe schlechter als das Regelwerk und wird NICHT aktiviert."
  echo "Die Bots spielen weiter regelbasiert. Mehr Situationen/Rollouts versuchen oder --activate-anyway."
  exit 4
fi
[ "$COMPARE" -eq 0 ] || [ "$COMPARE" -eq 4 ] || { echo "compare fehlgeschlagen (Exit-Code $COMPARE)." >&2; exit 1; }

trainer activate --model "$MODEL_ID"
echo; echo "Fertig: Modell $MODEL_ID (Datensatz $DATASET_ID) ist aktiv. Neu gestartete Bot-Spiele nutzen es."
