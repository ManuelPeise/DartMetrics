<#
.SYNOPSIS
    Baut die DartMetrics-Container neu und startet sie aktualisiert.

.DESCRIPTION
    - baut das API-Image (Web.Core) aus dem aktuellen Quellcode neu
    - holt neue Versionen der fertigen Images (MySQL, Mailpit), wenn -Pull angegeben ist
    - ersetzt die laufenden Container; die Daten (Volume mysql_data) bleiben erhalten
    - wartet, bis die API "healthy" ist, und zeigt Status + letzte Log-Zeilen

.EXAMPLE
    .\scripts\update-containers.ps1                 # nur die API neu bauen + neu starten
    .\scripts\update-containers.ps1 -All            # alle Dienste aktualisieren
    .\scripts\update-containers.ps1 -All -Pull      # zusätzlich neue MySQL/Mailpit-Images holen
    .\scripts\update-containers.ps1 -NoCache        # API ohne Build-Cache bauen
#>
[CmdletBinding()]
param(
    [switch] $All,
    [switch] $Pull,
    [switch] $NoCache,
    [int]    $TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'

# Immer im Lösungsordner arbeiten (dort liegen docker-compose.yml und .env)
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Invoke-Step([string] $title, [scriptblock] $action) {
    Write-Host "`n==> $title" -ForegroundColor Cyan
    & $action
    if ($LASTEXITCODE -ne 0) { throw "Fehlgeschlagen: $title (Exit-Code $LASTEXITCODE)" }
}

# Docker erreichbar?
docker info *> $null
if ($LASTEXITCODE -ne 0) { throw 'Docker läuft nicht. Bitte Docker Desktop starten.' }

if (-not (Test-Path (Join-Path $root '.env'))) { throw '.env fehlt im Lösungsordner.' }

$services = if ($All) { @() } else { @('api') }   # leer = alle Dienste

if ($Pull) {
    Invoke-Step 'Neue Basis-Images holen' { docker compose pull --ignore-buildable @services }
}

$buildArgs = @('compose', 'build')
if ($NoCache) { $buildArgs += '--no-cache' }
$buildArgs += '--pull'
$buildArgs += 'api'
Invoke-Step 'API-Image bauen' { docker @buildArgs }

Invoke-Step 'Container ersetzen' { docker compose up -d --force-recreate --remove-orphans @services }

# Auf die API warten
Write-Host "`n==> Warte auf API (max. $TimeoutSeconds s)" -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    Start-Sleep -Seconds 3
    $state = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' dartmetrics-api 2>$null
    Write-Host "    api: $state"
} while ($state -notin @('healthy', 'exited', 'dead') -and (Get-Date) -lt $deadline)

Invoke-Step 'Alte, unbenutzte Images entfernen' { docker image prune -f }

Write-Host "`n==> Status" -ForegroundColor Cyan
docker compose ps

Write-Host "`n==> Letzte API-Logs" -ForegroundColor Cyan
docker compose logs --tail 30 api

if ($state -ne 'healthy') {
    Write-Host "`nAPI ist nicht healthy (Status: $state). Logs oben prüfen." -ForegroundColor Red
    exit 1
}

Write-Host "`nFertig." -ForegroundColor Green
Write-Host '  API      http://localhost:8080/swagger'
Write-Host '  Mailpit  http://localhost:8025'
