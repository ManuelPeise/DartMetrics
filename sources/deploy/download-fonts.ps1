<#
.SYNOPSIS
    Lädt die Schriften der Web-App einmalig nach Web.App/Web.App/wwwroot/fonts (lokal ausgeliefert, DSGVO).
.EXAMPLE
    .\deploy\download-fonts.ps1
#>
$ErrorActionPreference = 'Stop'
$target = Join-Path (Split-Path -Parent $PSScriptRoot) 'Web.App\Web.App\wwwroot\fonts'
New-Item -ItemType Directory -Force -Path $target | Out-Null

$fonts = @(
    'barlow-condensed/latin-600-normal', 'barlow-condensed/latin-700-normal',
    'caveat/latin-500-normal', 'caveat/latin-700-normal',
    'inter/latin-400-normal', 'inter/latin-600-normal'
)

foreach ($font in $fonts) {
    $family, $variant = $font.Split('/')
    $file = Join-Path $target "$family-$variant.woff2"
    $url = "https://cdn.jsdelivr.net/fontsource/fonts/$family@latest/$variant.woff2"
    Write-Host "  $url"
    Invoke-WebRequest -Uri $url -OutFile $file
}

Write-Host "Fertig: $target" -ForegroundColor Green
