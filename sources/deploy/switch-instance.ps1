<#
.SYNOPSIS
    Startet die DartMetrics-Instanz dieses Ordners und stoppt alle anderen (ohne Neubau).
.DESCRIPTION
    Alle Instanzen (z. B. Arbeitskopie und Beta-Worktree) nutzen dieselben Ports, es läuft immer nur eine.
    Daten bleiben erhalten: Die andere Instanz wird nur gestoppt, nicht entfernt.
    Für neuen Code: deploy\update-containers.ps1 (mit Datenbank-Backup).
.EXAMPLE
    D:\WorkBench\DartMetrics-beta\sources\deploy\switch-instance.ps1     # zur Beta wechseln
    D:\WorkBench\DartMetrics\sources\deploy\switch-instance.ps1          # zurück zur Arbeitskopie
    .\deploy\switch-instance.ps1 -Production                             # nur docker-compose.yml
#>
[CmdletBinding()]
param(
    [switch] $Production,
    [int]    $TimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
. (Join-Path $PSScriptRoot 'instance.ps1')

if ($Production) { $env:COMPOSE_FILE = 'docker-compose.yml' }

docker info *> $null
if ($LASTEXITCODE -ne 0) { Write-Host 'Docker läuft nicht. Bitte Docker Desktop starten.' -ForegroundColor Red; exit 1 }
if (-not (Test-Path (Join-Path $root '.env'))) { Write-Host '.env fehlt im Lösungsordner (zuerst deploy\deploy.ps1).' -ForegroundColor Red; exit 1 }

$instance = Get-InstanceName $root
Write-Host "`n==> Wechsel zur Instanz '$instance' ($root)" -ForegroundColor Cyan
Stop-OtherInstances $instance

docker compose up -d --remove-orphans
if ($LASTEXITCODE -ne 0) { Write-Host 'Start fehlgeschlagen.' -ForegroundColor Red; exit 1 }

Write-Host "`n==> Warten auf API und Web-App (max. $TimeoutSeconds s)" -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    Start-Sleep -Seconds 3
    $api = Get-ServiceState 'api'
    $web = Get-ServiceState "web"
    Write-Host "    api: $api, web: $web"
} while (($api -ne 'healthy' -or $web -ne 'running') -and (Get-Date) -lt $deadline)

docker compose ps

if ($api -ne 'healthy' -or $web -ne 'running') {
    docker compose logs --tail 40 api web
    Write-Host "`nAPI oder Web-App laufen nicht. Logs oben prüfen." -ForegroundColor Red
    exit 1
}

Write-Host "`nInstanz '$instance' läuft: https://localhost:7254" -ForegroundColor Green
