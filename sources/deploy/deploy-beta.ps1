<#
.SYNOPSIS
    Beta-Instanz von DartMetrics in Docker deployen, parallel zur normalen Installation.

.DESCRIPTION
    Projekt "dartmetrics-beta" mit eigenen Containern, Volumes (eigene Datenbank) und Ports.
    Konfiguration aus .env.beta (wird beim ersten Lauf aus .env kopiert; Ports/WEB_APP_PORT bitte prüfen).
      1. Voraussetzungen: Docker, .env.beta ohne Platzhalter, Zertifikat
      2. Images bauen (Tag :beta)
      3. Container starten, auf API und Web-App warten

.EXAMPLE
    .\deploy\deploy-beta.ps1              # bauen und starten
    .\deploy\deploy-beta.ps1 -NoCache     # ohne Build-Cache
    .\deploy\deploy-beta.ps1 -Reset       # Beta inkl. Datenbank löschen und neu aufsetzen
    .\deploy\deploy-beta.ps1 -Remove      # Beta komplett entfernen (Container, Volumes, Images)
#>
[CmdletBinding()]
param(
    [switch] $NoCache,
    [switch] $Reset,
    [switch] $Remove,
    [int]    $TimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Step([string] $title) { Write-Host "`n==> $title" -ForegroundColor Cyan }
function Fail([string] $message) { Write-Host "`n$message" -ForegroundColor Red; exit 1 }

function Read-EnvFile([string] $path) {
    $values = @{}
    foreach ($line in Get-Content $path) {
        $trimmed = $line.Trim()
        if ($trimmed -eq '' -or $trimmed.StartsWith('#') -or -not $trimmed.Contains('=')) { continue }
        $key, $value = $trimmed.Split('=', 2)
        $values[$key.Trim()] = $value.Trim().Trim('"')
    }
    return $values
}

$envPath = Join-Path $root '.env.beta'
$compose = @('compose', '--env-file', $envPath, '-f', 'docker-compose.yml', '-f', 'docker-compose.beta.yml')

docker info *> $null
if ($LASTEXITCODE -ne 0) { Fail 'Docker läuft nicht. Bitte Docker Desktop starten.' }

if ($Remove) {
    Step 'Beta entfernen (Container, Volumes, Images)'
    docker @compose down -v --rmi all --remove-orphans
    Write-Host "`nBeta entfernt." -ForegroundColor Green
    exit 0
}

# ---------- 1. Voraussetzungen ----------
Step 'Voraussetzungen prüfen'

if (-not (Test-Path $envPath)) {
    $source = if (Test-Path (Join-Path $root '.env')) { '.env' } else { '.env.example' }
    Copy-Item (Join-Path $root $source) $envPath
    Add-Content $envPath "`n# Beta-Instanz`nBETA_HTTPS_PORT=7354`nBETA_HTTP_PORT=5223`nWEB_APP_PORT=7354"
    Fail ".env.beta wurde aus $source angelegt. Bitte prüfen (eigene Passwörter, Ports, WEB_APP_HOST/WEB_APP_PORT) und das Skript erneut starten:`n  $envPath"
}

$envValues = Read-EnvFile $envPath
$required = 'MYSQL_ROOT_PASSWORD', 'MYSQL_PASSWORD', 'JWT_KEY', 'ADMIN_PASSWORD', 'CERT_PASSWORD'
$missing = $required | Where-Object { -not $envValues[$_] -or $envValues[$_] -like 'change-me*' }
if ($missing) { Fail "In der .env.beta fehlen oder sind noch Platzhalter: $($missing -join ', ')" }
if ($envValues['JWT_KEY'].Length -lt 32) { Fail 'JWT_KEY muss mindestens 32 Zeichen lang sein.' }

$httpsPort = if ($envValues['BETA_HTTPS_PORT']) { $envValues['BETA_HTTPS_PORT'] } else { '7354' }
if ($envValues['WEB_APP_PORT'] -ne $httpsPort) {
    Write-Host "    Hinweis: WEB_APP_PORT ($($envValues['WEB_APP_PORT'])) passt nicht zu BETA_HTTPS_PORT ($httpsPort) - Links in E-Mails zeigen dann auf die falsche Instanz." -ForegroundColor Yellow
}
Write-Host '    .env.beta ok'

$cert = Join-Path $root 'certs\dartmetrics.pfx'
if (-not (Test-Path $cert)) { Fail "HTTPS-Zertifikat fehlt: $cert (einmal deploy.ps1 ausführen oder Zertifikat ablegen)." }
Write-Host '    Zertifikat ok'

if ($Reset) {
    Step 'Beta zurücksetzen (Container und Datenbank löschen)'
    docker @compose down -v --remove-orphans
}

# ---------- 2. Build ----------
Step 'Images bauen (api, web :beta)'
$buildArgs = $compose + @('build', '--pull')
if ($NoCache) { $buildArgs += '--no-cache' }
docker @buildArgs api web
if ($LASTEXITCODE -ne 0) { Fail 'Build fehlgeschlagen.' }

# ---------- 3. Start ----------
Step 'Container starten'
docker @compose up -d --remove-orphans
if ($LASTEXITCODE -ne 0) { Fail 'Start fehlgeschlagen.' }

Step "Warten auf API und Web-App (max. $TimeoutSeconds s)"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    Start-Sleep -Seconds 3
    $api = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' dartmetrics-beta-api 2>$null
    $web = docker inspect --format '{{.State.Status}}' dartmetrics-beta-web 2>$null
    Write-Host "    api: $api, web: $web"
} while (($api -ne 'healthy' -or $web -ne 'running') -and (Get-Date) -lt $deadline)

if ($api -ne 'healthy' -or $web -ne 'running') {
    docker @compose logs --tail 40 api web
    Fail 'API oder Web-App laufen nicht. Logs oben prüfen.'
}

Step 'Status'
docker @compose ps

Write-Host "`nBeta läuft." -ForegroundColor Green
Write-Host "  Web-App  https://localhost:$httpsPort"
$adminEmail = if ($envValues['ADMIN_EMAIL']) { $envValues['ADMIN_EMAIL'] } else { 'admin@dart.local' }
Write-Host "  Admin    $adminEmail (Passwort aus der .env.beta, beim ersten Login ändern)"
