<#
.SYNOPSIS
    Gemeinsame Funktionen der Deploy-Skripte für Instanzen (wird per Dot-Sourcing geladen).
.DESCRIPTION
    Eine Instanz = ein Ordner mit eigener .env (COMPOSE_PROJECT_NAME), eigenen Containern und Volumes.
    Alle Instanzen nutzen dieselben Ports, deshalb läuft immer nur eine: vor dem Start werden die anderen gestoppt.
    Container werden über "docker compose" (Dienstname) angesprochen, nie über feste Namen.
#>

function Get-InstanceName([string] $root) {
    $envPath = Join-Path $root '.env'
    if (Test-Path $envPath) {
        foreach ($line in Get-Content $envPath) {
            if ($line -match '^\s*COMPOSE_PROJECT_NAME\s*=\s*"?([^"#\s]+)"?') {
                return $Matches[1]
            }
        }
    }

    # Wie docker compose: ohne Angabe der Ordnername (bisher "sources").
    return (Split-Path $root -Leaf).ToLowerInvariant()
}

# Laufende DartMetrics-Instanzen (Compose-Projektnamen). Erkennt auch Container aus der Zeit vor den Instanzen
# (ohne Label, Image dartmetrics-api/-web).
function Get-RunningInstances {
    # Ohne Anführungszeichen im Format: Windows PowerShell 5.1 würde sie beim Aufruf von docker entfernen.
    $lines = docker ps --format '{{.Image}}|{{.Labels}}'
    $projects = foreach ($line in $lines) {
        $image, $labelText = $line -split '\|', 2
        $labels = @{}
        foreach ($pair in ($labelText -split ',')) {
            $key, $value = $pair -split '=', 2
            if ($key) { $labels[$key] = $value }
        }

        $project = $labels['com.docker.compose.project']
        if ($project -and ($labels['dartmetrics.instance'] -eq 'true' -or $image -like 'dartmetrics-*')) { $project }
    }
    return @($projects | Sort-Object -Unique)
}

function Stop-OtherInstances([string] $instance) {
    foreach ($other in Get-RunningInstances | Where-Object { $_ -ne $instance }) {
        Write-Host "    Andere Instanz '$other' läuft - wird gestoppt (Daten bleiben erhalten)." -ForegroundColor Yellow
        docker compose -p $other stop
        if ($LASTEXITCODE -ne 0) { throw "Instanz '$other' konnte nicht gestoppt werden." }
    }
}

# Status eines Dienstes dieser Instanz: health (healthy/starting/unhealthy), sonst running/exited/…; leer = kein Container.
function Get-ServiceState([string] $service) {
    $id = docker compose ps -a -q $service 2>$null | Select-Object -First 1
    if (-not $id) { return '' }
    return docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' $id 2>$null
}
