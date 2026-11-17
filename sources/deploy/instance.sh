#!/usr/bin/env bash
# Gemeinsame Funktionen der Deploy-Skripte für Instanzen (wird mit "source" geladen).
# Eine Instanz = ein Ordner mit eigener .env (COMPOSE_PROJECT_NAME), eigenen Containern und Volumes.
# Alle Instanzen nutzen dieselben Ports, deshalb läuft immer nur eine: vor dem Start werden die anderen gestoppt.
# Container werden über "docker compose" (Dienstname) angesprochen, nie über feste Namen.

instance_name() {
  local name
  name=$(grep -E '^\s*COMPOSE_PROJECT_NAME\s*=' .env 2>/dev/null | tail -n1 | cut -d= -f2- | sed 's/^[ "]*//; s/[ "]*$//' || true)
  # Wie docker compose: ohne Angabe der Ordnername (bisher "sources").
  [ -n "$name" ] || name=$(basename "$PWD" | tr '[:upper:]' '[:lower:]')
  echo "$name"
}

# Laufende DartMetrics-Instanzen; erkennt auch alte Container ohne Label (Image dartmetrics-api/-web).
running_instances() {
  docker ps --format '{{.Label "com.docker.compose.project"}}|{{.Label "dartmetrics.instance"}}|{{.Image}}' |
    awk -F'|' '$1 != "" && ($2 == "true" || $3 ~ /^dartmetrics-/) { print $1 }' | sort -u
}

stop_other_instances() {
  local own="$1" other
  for other in $(running_instances); do
    [ "$other" = "$own" ] && continue
    echo "    Andere Instanz '$other' läuft - wird gestoppt (Daten bleiben erhalten)."
    docker compose -p "$other" stop
  done
}

# Status eines Dienstes dieser Instanz: health, sonst running/exited/…; leer = kein Container.
service_state() {
  local id
  id=$(docker compose ps -a -q "$1" 2>/dev/null | head -n1)
  [ -n "$id" ] || { echo ""; return; }
  docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$id" 2>/dev/null || true
}
