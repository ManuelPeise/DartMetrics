<#
.SYNOPSIS
    Erzeugt ein HTTPS-Zertifikat für DartMetrics, dem PC und Handy vertrauen (keine Browser-Warnung mehr).
.DESCRIPTION
    1. Legt einmalig eine eigene Zertifizierungsstelle an ("DartMetrics Local CA", 10 Jahre) und vertraut ihr
       für den aktuellen Windows-Benutzer (Windows fragt einmal nach).
    2. Stellt damit certs\dartmetrics.pfx aus (825 Tage) für localhost, den Rechnernamen und die LAN-IPs
       (bzw. die mit -Hosts angegebenen Namen/IPs). Passwort: CERT_PASSWORD aus der .env.
    3. Exportiert das CA-Zertifikat nach certs\dartmetrics-ca.cer - das kommt aufs Handy (einmalig).
    Ein vorhandenes certs\dartmetrics.pfx wird nach certs\dartmetrics.pfx.bak gesichert.
    Danach: docker compose up -d --force-recreate web
.EXAMPLE
    .\deploy\create-local-cert.ps1                                  # localhost, Rechnername, alle LAN-IPs
    .\deploy\create-local-cert.ps1 -Hosts localhost,192.168.178.46  # nur diese Namen/IPs
.NOTES
    Handy (einmalig): certs\dartmetrics-ca.cer aufs Handy kopieren und installieren
      Android: Einstellungen > Sicherheit > Verschlüsselung & Anmeldedaten > Zertifikat installieren > CA-Zertifikat
      iPhone:  Datei öffnen > Einstellungen > Profil installieren, dann Allgemein > Info > Zertifikatsvertrauen > aktivieren
    Der private Schlüssel der CA bleibt im Windows-Zertifikatspeicher (CurrentUser\My) und verlässt den PC nicht.
#>
[CmdletBinding()]
param(
    [string[]] $Hosts
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$caSubject = 'CN=DartMetrics Local CA'
$certDir = Join-Path $root 'certs'
$pfxPath = Join-Path $certDir 'dartmetrics.pfx'
$caPath = Join-Path $certDir 'dartmetrics-ca.cer'

# ---------- Passwort aus der .env ----------
$envPath = Join-Path $root '.env'
if (-not (Test-Path $envPath)) { throw '.env fehlt im Lösungsordner (CERT_PASSWORD wird gebraucht).' }
$password = (Get-Content $envPath | Where-Object { $_ -match '^\s*CERT_PASSWORD\s*=' } | Select-Object -Last 1) -replace '^\s*CERT_PASSWORD\s*=\s*', ''
$password = $password.Trim().Trim('"')
if (-not $password -or $password -like 'change-me*') { throw 'CERT_PASSWORD ist in der .env nicht gesetzt.' }
$securePassword = ConvertTo-SecureString $password -AsPlainText -Force

# ---------- Namen und IPs ----------
if (-not $Hosts) {
    $ips = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -match '^(10\.|192\.168\.|172\.(1[6-9]|2\d|3[01])\.)' -and $_.PrefixOrigin -ne 'WellKnown' } |
        Select-Object -ExpandProperty IPAddress
    $Hosts = @('localhost', '127.0.0.1', $env:COMPUTERNAME.ToLowerInvariant()) + @($ips)
}
$Hosts = $Hosts | Where-Object { $_ } | Select-Object -Unique

$san = ($Hosts | ForEach-Object {
    if ($_ -as [System.Net.IPAddress]) { "IPAddress=$_" } else { "DNS=$_" }
}) -join '&'

Write-Host "`n==> Zertifikat für: $($Hosts -join ', ')" -ForegroundColor Cyan

# ---------- 1. Zertifizierungsstelle ----------
$ca = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq $caSubject -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date).AddDays(30) } |
    Sort-Object NotAfter -Descending | Select-Object -First 1

if ($ca) {
    Write-Host "    Vorhandene CA wird verwendet (gültig bis $($ca.NotAfter.ToString('d')))."
}
else {
    Write-Host '    Neue CA "DartMetrics Local CA" wird angelegt.'
    $ca = New-SelfSignedCertificate -Type Custom -Subject $caSubject `
        -KeyUsage CertSign, CRLSign, DigitalSignature -KeyUsageProperty Sign `
        -TextExtension @('2.5.29.19={critical}{text}ca=1&pathlength=0') `
        -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable `
        -NotAfter (Get-Date).AddYears(10) -CertStoreLocation Cert:\CurrentUser\My
}

New-Item -ItemType Directory -Force -Path $certDir | Out-Null
Export-Certificate -Cert $ca -FilePath $caPath -Type CERT | Out-Null

# Dem PC beibringen, der CA zu vertrauen (Windows zeigt dafür einmal eine Sicherheitsabfrage).
$trusted = Get-ChildItem Cert:\CurrentUser\Root | Where-Object { $_.Thumbprint -eq $ca.Thumbprint }
if (-not $trusted) {
    Write-Host '    CA wird als vertrauenswürdig eingetragen - bitte die Windows-Abfrage mit "Ja" bestätigen.' -ForegroundColor Yellow
    Import-Certificate -FilePath $caPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
}

# ---------- 2. Server-Zertifikat ----------
$server = New-SelfSignedCertificate -Type Custom -Subject 'CN=DartMetrics' -Signer $ca `
    -TextExtension @("2.5.29.17={text}$san", '2.5.29.37={text}1.3.6.1.5.5.7.3.1') `
    -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddDays(825) -CertStoreLocation Cert:\CurrentUser\My

if (Test-Path $pfxPath) {
    Copy-Item $pfxPath "$pfxPath.bak" -Force
    Write-Host "    Altes Zertifikat gesichert: $pfxPath.bak"
}

# AES256: von .NET/OpenSSL im Linux-Container zuverlässig lesbar (ältere RC2/3DES-Verschlüsselung nicht immer).
Export-PfxCertificate -Cert $server -FilePath $pfxPath -Password $securePassword -ChainOption BuildChain `
    -CryptoAlgorithmOption AES256_SHA256 | Out-Null

# Das Server-Zertifikat liegt jetzt in der .pfx; im Windows-Speicher wird es nicht gebraucht.
Remove-Item "Cert:\CurrentUser\My\$($server.Thumbprint)" -DeleteKey

Write-Host "`nFertig." -ForegroundColor Green
Write-Host "  Server-Zertifikat  $pfxPath (gültig bis $($server.NotAfter.ToString('d')))"
Write-Host "  CA fürs Handy      $caPath"
Write-Host "`nNächste Schritte:"
Write-Host '  1. docker compose up -d --force-recreate web'
Write-Host '  2. dartmetrics-ca.cer einmalig aufs Handy kopieren und als CA-Zertifikat installieren (siehe Kopf des Skripts).'
Write-Host '  3. Browser neu starten.'
