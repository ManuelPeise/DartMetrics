# DartMetrics – Technische Dokumentation

Setup, Betrieb und Entwicklung. Für Spieler und Admins: **[Benutzerhandbuch](../docs/user-guide.md)** · Projektüberblick: **[README im Hauptordner](../README.md)**.

| Dokument | Inhalt |
|---|---|
| Diese Datei | Voraussetzungen, Schnellstart, Deployment, Skripte, Betrieb einer eigenen Instanz |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Projekte, Abhängigkeiten, Laufzeit, Service-Verzeichnis, Web-Bereiche, API |
| [CLAUDE.md](CLAUDE.md) | Entwicklungsregeln und Konventionen |
| `*/README.md` je Projekt | Details zu Aufbau und Regeln des Projekts |
| [CONTRIBUTING.md](../CONTRIBUTING.md) | Mitarbeit: Branches, Commits, Tests, Pull Requests |

## Technik

- **.NET 10**, ASP.NET Core Web API, **Blazor** (WebAssembly für Spieler, Interactive Server für die Administration, statische Seiten für Impressum/Datenschutz), Backend-for-Frontend mit Cookie-Anmeldung; JWT nur zwischen Web-App und API.
- **MySQL 8.4** mit Entity Framework Core (Oracle-Provider), Migrationen beim Start.
- **Docker Compose** für Web-App, API, Datenbank, Mailpit (Entwicklung) und den ML-Trainer.
- Tests: xUnit, Testcontainers (MySQL), GitHub Actions.

## Voraussetzungen

- Docker (Docker Desktop unter Windows/macOS, Docker Engine + Compose-Plugin unter Linux/Raspberry Pi OS 64 Bit)
- Für die Entwicklung: .NET SDK 10
- Windows: PowerShell für die `.ps1`-Skripte; Linux: bash, `openssl`, `curl`

## Schnellstart

Am einfachsten mit dem Deployment-Skript (prüft alles, legt Zertifikat und Schriften an, baut und startet):

```
cd sources                            # alle Befehle im Ordner sources
cp .env.example .env                  # Platzhalter ersetzen
./deploy/deploy.ps1                   # Windows   (Linux/Pi: ./deploy/deploy.sh)
```

Oder Schritt für Schritt:

```
cp .env.example .env                  # Platzhalter ersetzen (Passwörter, JWT_KEY, CERT_PASSWORD, LEGAL_*)
./deploy/download-fonts.ps1           # Schriften einmalig lokal ablegen (kein Google Fonts, DSGVO)
dotnet dev-certs https -ep certs/dartmetrics.pfx -p <CERT_PASSWORD>
dotnet dev-certs https --trust
docker compose up -d                  # MySQL, Mailpit, API, Web-App
```

- Web-App: https://localhost:7254 · Swagger (nur Entwicklung): http://localhost:8080/swagger · Mails: http://localhost:8025
- `docker compose` lädt in der Entwicklung automatisch `docker-compose.override.yml` (offene Ports nur auf `127.0.0.1`, Mailpit, Swagger).
- Produktion (z. B. Raspberry Pi): in der `.env` `COMPOSE_FILE=docker-compose.yml` setzen. Dann ist nur die Web-App erreichbar; MySQL und API laufen nur im Docker-Netz. Backups: `deploy/backup-db.sh` per Cron.
- Impressum und Datenschutzerklärung: `/impressum`, `/datenschutz`. Die Angaben zum Betreiber kommen aus `LEGAL_*` in der `.env`, nicht aus dem Repository.
- Geheimnisse stehen nur in `.env` (nicht im Repository); alle Variablen mit Erklärung: `.env.example`.
- Web-App ohne Docker (Debugging): `dotnet run --project Web.App/Web.App`
- API aktualisieren: `deploy/update-containers.ps1` (Windows) bzw. `deploy/update-containers.sh`.

## Deployment und Skripte

| Skript (`.ps1` Windows / `.sh` Linux, Pi) | Zweck |
| --- | --- |
| `deploy/deploy` | Erstinstallation bzw. kompletter Neuaufbau: prüft `.env`, Zertifikat und Schriften (legt fehlende an), baut alle Images, startet, wartet auf API/Web-App |
| `deploy/update-containers` | Code-Update: Datenbank-Backup, API, Web-App und Trainer-Worker neu bauen und ersetzen. |
| `deploy/switch-instance` | Instanz dieses Ordners starten (ohne Neubau), andere Instanzen stoppen. |
| `deploy/backup-db` (`.sh`) | Tägliches Datenbank-Backup per Cron, mit Rotation. |
| `deploy/download-fonts` | Schriften einmalig lokal ablegen. |
| `deploy/instance` | Gemeinsame Funktionen der Skripte (Instanzname, andere Instanzen stoppen, Dienststatus). |

Alle Skripte gelten für die **Instanz des Ordners**, in dem sie liegen, und sprechen die Container über
`docker compose` an (keine festen Containernamen).

### Instanzen (z. B. Beta)

Jeder Ordner mit eigener `.env` ist eine eigene Instanz mit allen Containern (MySQL, API, Web-App, Mailpit) und eigenen
Volumes, also eigener Datenbank. Der Name steht in `COMPOSE_PROJECT_NAME` der `.env` (ohne Angabe: Ordnername, bisher
`sources`). Alle Instanzen nutzen **dieselben Ports** (7254, 5123 …), deshalb läuft immer nur eine:
`deploy`, `update-containers` und `switch-instance` stoppen andere laufende DartMetrics-Instanzen vorher (nur stoppen,
Daten bleiben).

Beta einrichten: eigener Ordner als `git worktree` des Beta-Branches. Er bleibt eingefroren, während `master` weitergeht.

```
cd D:\WorkBench\DartMetrics
git worktree add ..\DartMetrics-beta beta/version-v0.0.1
copy sources\.env ..\DartMetrics-beta\sources\.env        # dann COMPOSE_PROJECT_NAME=dartmetrics-beta setzen
copy sources\certs\dartmetrics.pfx ..\DartMetrics-beta\sources\certs\
..\DartMetrics-beta\sources\deploy\deploy.ps1             # baut und startet die Beta, stoppt die andere Instanz
```

- Neuer Beta-Stand: im Beta-Worktree mergen (`git merge master`), dann `deploy\update-containers.ps1` (mit Backup).
- Wechseln ohne Neubau: `deploy\switch-instance.ps1` im Ordner der gewünschten Instanz.
- Mit `COMPOSE_PROJECT_NAME=dartmetrics-beta` wird die bestehende Beta-Datenbank (Volume `dartmetrics-beta_mysql_data`) weiterverwendet.
- Die Arbeitskopie ohne `COMPOSE_PROJECT_NAME` (bzw. mit `sources`) behält ihre bisherigen Volumes (`sources_mysql_data` …).
  Vorher mit `docker volume ls` prüfen, wie die Volumes heißen, und den Namen passend setzen.

**Umstellung von festen Containernamen (einmalig):** Container aus der Zeit vor den Instanzen (`mysql`, `mailpit`,
`dartmetrics-api`, `dartmetrics-web`, `dartmetrics-beta-*`) einmal entfernen, die Volumes bleiben erhalten:
`docker compose down` in der Arbeitskopie bzw. `docker compose -p dartmetrics-beta down` für die alte Beta.


## Eigene Instanz betreiben

DartMetrics ist Open Source (MIT-Lizenz) und wird ohne Gewähr bereitgestellt. **Wer eine Instanz betreibt, ist dafür selbst verantwortlich**, rechtlich (Impressum, Datenschutz, DSGVO) und technisch (Updates, Backups, HTTPS).

- Impressum und Datenschutzerklärung (`/impressum`, `/datenschutz`) sind **Vorlagen**. Sie beschreiben, was die App ab Werk speichert, ersetzen aber keine rechtliche Prüfung. Bitte prüfen und an die eigene Instanz anpassen, vor allem bei geändertem Code, eigenem Mail-Anbieter oder öffentlichem Betrieb.
- Die Angaben zum Betreiber kommen aus `LEGAL_*` in der `.env` (siehe `.env.example`). Im Repository stehen keine persönlichen Daten.
- Sicherheitslücken bitte vertraulich melden: siehe [SECURITY.md](../SECURITY.md).

## Tests

```
dotnet test DartMetrics.slnx          # Unit- und Integrationstests (Integrationstests brauchen Docker)
```

Die GitHub-Action `.github/workflows/build.yml` baut die Solution, führt alle Tests aus und baut die Docker-Images bei jedem Push und Pull Request.
