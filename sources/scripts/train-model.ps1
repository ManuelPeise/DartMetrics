<#
.SYNOPSIS
    Erzeugt, trainiert, prüft und aktiviert das ML-Zielwahl-Modell der DartBots - komplett in Docker.

.DESCRIPTION
    Nach einem frischen Deployment gibt es kein Modell; die Bots spielen dann regelbasiert (funktioniert, aber schwächer).
    Dieses Skript führt alle Schritte von Tools.BotTrainer im Container "bottrainer" aus:
      1. generate  Datensatz aus simulierten Situationen        -> ml_data/datasets
      2. train     Modell trainieren                            -> ml_data/models
      3. compare   gegen das Regelwerk in simulierten Legs prüfen
      4. activate  nur, wenn das Modell auf keiner Stufe signifikant schlechter ist
    Voraussetzung: MySQL und API laufen und die Migrationen sind angewendet (scripts/deploy.ps1 bzw. docker compose up -d).

.EXAMPLE
    .\scripts\train-model.ps1                         # nur, wenn noch kein Modell aktiv ist
    .\scripts\train-model.ps1 -Force                  # neues Modell trainieren, auch wenn schon eins aktiv ist
    .\scripts\train-model.ps1 -Situations 300 -Rollouts 30 -Legs 500    # schneller Probelauf (schwächeres Modell)
    .\scripts\train-model.ps1 -ActivateAnyway         # auch aktivieren, wenn der Vergleich schlechter ausfällt

.NOTES
    Dauer mit Standardwerten: grob 10-30 Minuten auf einem PC, auf einem Raspberry Pi deutlich länger.
    Tipp für den Pi: auf dem PC trainieren und den Ordner ml_data plus Datenbank-Einträge per Backup übertragen,
    oder mit kleineren Werten trainieren.
#>
[CmdletBinding()]
param(
    [int]    $Situations = 1000,
    [int]    $Rollouts = 50,
    [int]    $Legs = 2000,
    [int]    $Seed = 1,
    [switch] $Force,
    [switch] $ActivateAnyway
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

function Invoke-Trainer([string[]] $arguments) {
    Write-Host "`n==> bottrainer $($arguments -join ' ')" -ForegroundColor Cyan
    # Ausgabe live anzeigen und gleichzeitig auswerten (Ids der neuen Datensätze/Modelle).
    docker compose --profile tools run --rm bottrainer @arguments 2>&1 | Tee-Object -Variable lines | Out-Host
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Text = ($lines | Out-String) }
}

function Get-ActiveModelCount {
    $count = docker exec mysql sh -c 'mysql -N -uroot -p"$MYSQL_ROOT_PASSWORD" DartContextDb -e "SELECT COUNT(*) FROM DartBotActiveModels" 2>/dev/null'
    if ($LASTEXITCODE -ne 0) { throw 'Datenbank nicht erreichbar oder Tabellen fehlen. Läuft "docker compose up -d" (API wendet die Migrationen an)?' }
    return [int]($count | Select-Object -Last 1)
}

docker info *> $null
if ($LASTEXITCODE -ne 0) { throw 'Docker läuft nicht. Bitte Docker Desktop starten.' }

if (-not $Force -and (Get-ActiveModelCount) -gt 0) {
    Write-Host 'Es ist bereits ein Zielwahl-Modell aktiv. Neu trainieren mit -Force.' -ForegroundColor Yellow
    exit 0
}

$commit = git rev-parse --short HEAD 2>$null
if ($LASTEXITCODE -eq 0) { $env:BOTTRAINER_COMMIT = $commit }

Write-Host "`n==> Trainer-Image bauen" -ForegroundColor Cyan
docker compose --profile tools build bottrainer
if ($LASTEXITCODE -ne 0) { throw 'Build des Trainer-Images fehlgeschlagen.' }

# 1. Datensatz
$generate = Invoke-Trainer @('generate', '--situations', $Situations, '--rollouts', $Rollouts, '--seed', $Seed)
if ($generate.ExitCode -ne 0) { throw "generate fehlgeschlagen (Exit-Code $($generate.ExitCode))." }
if ($generate.Text -notmatch 'DartBotTrainingDatasets\.Id = (\d+)') { throw 'Datensatz-Id nicht in der Ausgabe gefunden.' }
$datasetId = $Matches[1]

# 2. Training
$train = Invoke-Trainer @('train', '--dataset', $datasetId, '--seed', $Seed)
if ($train.ExitCode -ne 0) { throw "train fehlgeschlagen (Exit-Code $($train.ExitCode))." }
if ($train.Text -notmatch 'DartBotModels: Id (\d+)') { throw 'Modell-Id nicht in der Ausgabe gefunden.' }
$modelId = $Matches[1]

# 3. Vergleich mit dem Regelwerk (Exit-Code 4 = auf einer Stufe signifikant schlechter)
$compare = Invoke-Trainer @('compare', '--model', $modelId, '--legs', $Legs, '--seed', $Seed)
if ($compare.ExitCode -eq 4 -and -not $ActivateAnyway) {
    Write-Host "`nModell $modelId ist auf mindestens einer Stufe schlechter als das Regelwerk und wird NICHT aktiviert." -ForegroundColor Yellow
    Write-Host 'Die Bots spielen weiter regelbasiert. Mehr Situationen/Rollouts versuchen oder mit -ActivateAnyway trotzdem aktivieren.'
    exit 4
}
if ($compare.ExitCode -ne 0 -and $compare.ExitCode -ne 4) { throw "compare fehlgeschlagen (Exit-Code $($compare.ExitCode))." }

# 4. Aktivieren
$activate = Invoke-Trainer @('activate', '--model', $modelId)
if ($activate.ExitCode -ne 0) { throw "activate fehlgeschlagen (Exit-Code $($activate.ExitCode))." }

Write-Host "`nFertig: Modell $modelId (Datensatz $datasetId) ist aktiv. Neu gestartete Bot-Spiele nutzen es." -ForegroundColor Green
