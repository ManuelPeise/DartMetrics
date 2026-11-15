<#
.SYNOPSIS
    Komplettes Deployment von DartMetrics in Docker: Prüfungen, Build, Start, Health-Check, optional ML-Modell.

.DESCRIPTION
    Für die Erstinstallation und für einen vollständigen Neuaufbau. Für reine Code-Updates reicht
    scripts/update-containers.ps1 (mit Datenbank-Backup).
      1. Voraussetzungen: Docker, .env (ohne Platzhalter), HTTPS-Zertifikat, lokale Schriften
      2. Images bauen: api, web, bottrainer
      3. Container starten und warten, bis API und Web-App laufen
      4. Falls noch kein ML-Zielwahl-Modell aktiv ist: scripts/train-model.ps1 (abschaltbar mit -SkipModel)

.EXAMPLE
    .\scripts\deploy.ps1                   # Entwicklung (lädt docker-compose.override.yml)
    .\scripts\deploy.ps1 -Production       # nur docker-compose.yml: keine offenen DB/API-Ports, kein Mailpit
    .\scripts\deploy.ps1 -SkipModel        # ohne ML-Training (Bots spielen regelbasiert)
    .\scripts\deploy.ps1 -NoCache          # Images ohne Build-Cache
#>
[CmdletBinding()]
param(
    [switch] $Production,
    [switch] $SkipModel,
    [switch] $NoCache,
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

if ($Production) { $env:COMPOSE_FILE = 'docker-compose.yml' }

# ---------- 1. Voraussetzungen ----------
Step 'Voraussetzungen prüfen'

docker info *> $null
if ($LASTEXITCODE -ne 0) { Fail 'Docker läuft nicht. Bitte Docker Desktop starten.' }

$envPath = Join-Path $root '.env'
if (-not (Test-Path $envPath)) {
    Copy-Item (Join-Path $root '.env.example') $envPath
    Fail ".env fehlte und wurde aus .env.example angelegt. Bitte die Platzhalter (change-me, LEGAL_*) ersetzen und das Skript erneut starten:`n  $envPath"
}

$envValues = Read-EnvFile $envPath
$required = 'MYSQL_ROOT_PASSWORD', 'MYSQL_PASSWORD', 'JWT_KEY', 'ADMIN_PASSWORD', 'CERT_PASSWORD'
$missing = $required | Where-Object { -not $envValues[$_] -or $envValues[$_] -like 'change-me*' }
if ($missing) { Fail "In der .env fehlen oder sind noch Platzhalter: $($missing -join ', ')" }
if ($envValues['JWT_KEY'].Length -lt 32) { Fail 'JWT_KEY muss mindestens 32 Zeichen lang sein.' }
if (-not $envValues['LEGAL_NAME'] -or $envValues['LEGAL_NAME'] -eq 'Max Mustermann') {
    Write-Host '    Hinweis: LEGAL_* (Impressum/Datenschutz) sind noch nicht gesetzt.' -ForegroundColor Yellow
}
Write-Host '    .env ok'

$cert = Join-Path $root 'certs\dartmetrics.pfx'
if (-not (Test-Path $cert)) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Fail "HTTPS-Zertifikat fehlt: $cert (dotnet SDK nicht gefunden, um ein Entwicklungszertifikat zu erzeugen)." }
    Step 'HTTPS-Entwicklungszertifikat erzeugen'
    New-Item -ItemType Directory -Force -Path (Split-Path $cert) | Out-Null
    dotnet dev-certs https -ep $cert -p $envValues['CERT_PASSWORD']
    if ($LASTEXITCODE -ne 0) { Fail 'Zertifikat konnte nicht erzeugt werden.' }
    dotnet dev-certs https --trust
}
Write-Host '    Zertifikat ok'

$fonts = Join-Path $root 'Web.App\Web.App\wwwroot\fonts'
if (-not (Get-ChildItem $fonts -Filter *.woff2 -ErrorAction SilentlyContinue)) {
    Step 'Schriften laden'
    & (Join-Path $PSScriptRoot 'download-fonts.ps1')
}
Write-Host '    Schriften ok'

# ---------- 2. Build ----------
Step 'Images bauen (api, web, bottrainer)'
$buildArgs = @('compose', '--profile', 'tools', 'build', '--pull')
if ($NoCache) { $buildArgs += '--no-cache' }
docker @buildArgs api web bottrainer
if ($LASTEXITCODE -ne 0) { Fail 'Build fehlgeschlagen.' }

# ---------- 3. Start ----------
Step 'Container starten'
docker compose up -d --remove-orphans
if ($LASTEXITCODE -ne 0) { Fail 'Start fehlgeschlagen.' }

Step "Warten auf API und Web-App (max. $TimeoutSeconds s)"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    Start-Sleep -Seconds 3
    $api = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' dartmetrics-api 2>$null
    $web = docker inspect --format '{{.State.Status}}' dartmetrics-web 2>$null
    Write-Host "    api: $api, web: $web"
} while (($api -ne 'healthy' -or $web -ne 'running') -and (Get-Date) -lt $deadline)

if ($api -ne 'healthy' -or $web -ne 'running') {
    docker compose logs --tail 40 api web
    Fail 'API oder Web-App laufen nicht. Logs oben prüfen.'
}

# ---------- 4. ML-Modell ----------
if (-not $SkipModel) {
    Step 'ML-Zielwahl-Modell'
    & (Join-Path $PSScriptRoot 'train-model.ps1')
    if ($LASTEXITCODE -eq 4) { Write-Host '    Kein Modell aktiviert - die Bots spielen regelbasiert.' -ForegroundColor Yellow }
}

# ---------- Fertig ----------
Step 'Status'
docker compose ps

Write-Host "`nFertig." -ForegroundColor Green
Write-Host '  Web-App  https://localhost:7254'
if (-not $Production) {
    Write-Host '  Swagger  http://localhost:8080/swagger'
    Write-Host '  Mailpit  http://localhost:8025'
}
$adminEmail = if ($envValues['ADMIN_EMAIL']) { $envValues['ADMIN_EMAIL'] } else { 'admin@dart.local' }
Write-Host "  Admin    $adminEmail (Passwort aus der .env, beim ersten Login ändern)"
