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
    .\scripts\update-containers.ps1 -Test           # vorher dotnet test (Abbruch bei Fehlern)
    .\scripts\update-containers.ps1 -SkipBackup     # ohne Datenbank-Backup

.NOTES
    Vor dem Ersetzen wird die Datenbank nach backups\ gesichert (die API wendet Migrationen beim Start an).
    Es bleiben die letzten -KeepBackups Sicherungen. Wiederherstellen:
      Get-Content backups\<datei>.sql | docker exec -i mysql sh -c 'exec mysql -uroot -p"$MYSQL_ROOT_PASSWORD" DartContextDb'
#>
[CmdletBinding()]
param(
    [switch] $All,
    [switch] $Pull,
    [switch] $NoCache,
    [switch] $Test,
    [switch] $SkipBackup,
    [int]    $KeepBackups = 10,
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

# [string[]] erzwingt ein Array: sonst wird ein einzelnes 'api' zum String, und @services
# splattet ihn zeichenweise ('a', 'p', 'i') -> "no such service: a".
[string[]] $services = if ($All) { @() } else { @('api') }   # leer = alle Dienste

if ($Test) {
    Invoke-Step 'Tests ausführen' { dotnet test DartMetrics.slnx --nologo }
}

if (-not $SkipBackup) {
    $mysqlRunning = docker inspect --format '{{.State.Running}}' mysql 2>$null
    if ($mysqlRunning -eq 'true') {
        $backupDir = Join-Path $root 'backups'
        New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
        $file = Join-Path $backupDir ("DartContextDb-{0}.sql" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))

        # Dump im Container in eine Datei schreiben und dann kopieren: PowerShell-Umleitung (>)
        # würde die Ausgabe als Text neu kodieren. Passwort bleibt im Container, --hex-blob für Profilbilder.
        Invoke-Step 'Datenbank sichern' {
            docker exec mysql sh -c 'mysqldump -uroot -p"$MYSQL_ROOT_PASSWORD" --single-transaction --routines --hex-blob DartContextDb > /tmp/backup.sql'
        }
        Invoke-Step 'Backup kopieren' { docker cp mysql:/tmp/backup.sql $file }
        docker exec mysql rm -f /tmp/backup.sql | Out-Null
        Write-Host ("    {0} ({1:N0} KB)" -f $file, ((Get-Item $file).Length / 1KB))

        Get-ChildItem $backupDir -Filter 'DartContextDb-*.sql' |
            Sort-Object LastWriteTime -Descending |
            Select-Object -Skip $KeepBackups |
            Remove-Item -Force
    }
    else {
        Write-Host "`nMySQL läuft nicht - kein Backup (erster Start?)." -ForegroundColor Yellow
    }
}

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

Write-Host "`n==> Angewendete Migrationen (dieser Start)" -ForegroundColor Cyan
$migrations = docker compose logs api 2>$null | Select-String 'Applying migration'
if ($migrations) { $migrations | ForEach-Object { Write-Host "    $_" } } else { Write-Host '    keine neuen' }

if ($state -ne 'healthy') {
    Write-Host "`nAPI ist nicht healthy (Status: $state). Logs oben prüfen." -ForegroundColor Red
    exit 1
}

Write-Host "`nFertig." -ForegroundColor Green
Write-Host '  API      http://localhost:8080/swagger'
Write-Host '  Mailpit  http://localhost:8025'
