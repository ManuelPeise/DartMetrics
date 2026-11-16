# Tools.Deployer

Windows-Oberfläche für das Docker-Deployment von DartMetrics (Docker Desktop, lokal). Gedacht für alle, die DartMetrics
installieren oder aktualisieren wollen, ohne PowerShell und Docker-Befehle zu kennen.

Die App enthält **keine eigene Deploy-Logik**: Sie bearbeitet die `.env` und ruft die Skripte in [`deploy/`](../deploy) auf.
Wer lieber die Konsole nimmt, kann die Skripte weiterhin direkt verwenden.

## Funktionen

| Bereich | Was passiert |
|---|---|
| **Umgebung** | *Entwicklung* (`.env`, mit `docker-compose.override.yml`), *Produktion* (`.env`, nur `docker-compose.yml`), *Beta* (`.env.beta`, parallele Instanz) |
| **1 · Einstellungen** | Formular aus der `.env.example`: Abschnitte und Kommentare werden zu Gruppen und Hilfetexten. Passwörter sind verdeckt, „Erzeugen“ legt zufällige Werte an. Dieselben Prüfungen wie `deploy.ps1` (Pflichtfelder, `change-me`, `JWT_KEY` ≥ 32 Zeichen), dazu Ports und Zahlen. Gespeichert werden nur geänderte Zeilen, Kommentare bleiben erhalten. |
| **2 · Deployment** | Aktionen, Schrittliste (aus den `==>`-Zeilen der Skripte), vollständiges Log, Abbrechen |
| **Status** | Ampel je Container (inkl. Healthcheck), Links zu Web-App, Swagger, Mailpit, Lösungsordner und Backups. Startet bei Bedarf Docker Desktop. |

### Aktionen

| Schaltfläche | Aufruf |
|---|---|
| Erstinstallation / Neuaufbau | `deploy\deploy.ps1` (`-Production`, `-NoCache`) |
| Aktualisieren (neuer Code) | `deploy\update-containers.ps1` (`-Test`, `-Pull`, `-All`, `-NoCache`, `-SkipBackup`) |
| Einstellungen übernehmen | `docker compose up -d --remove-orphans` – kein Rebuild, nur Container mit geänderter Konfiguration werden ersetzt |
| Starten / Stoppen / Logs | `docker compose start` / `stop` / `logs --tail 150 api web` |
| Beta bauen und starten | `deploy\deploy-beta.ps1` |
| Beta zurücksetzen / entfernen | `deploy\deploy-beta.ps1 -Reset` / `-Remove` (mit Rückfrage) |

Für *Produktion* setzt die App `COMPOSE_FILE=docker-compose.yml` für den jeweiligen Aufruf – wie `deploy.ps1 -Production`.

## Warum Konfiguration nur einen Neustart braucht

Die `appsettings.json` ist im Image und liefert die Standardwerte. Alles, was sich je Installation unterscheidet, kommt
über die `.env` als Umgebungsvariable in den Container (`docker-compose.yml`, z. B. `Jwt__Key: ${JWT_KEY}`) und
überschreibt die JSON-Werte. Eine Änderung in den Einstellungen braucht deshalb nur „Einstellungen übernehmen“.
Neu bauen muss man nur bei neuem Code.

Ausnahme: `MYSQL_ROOT_PASSWORD`, `MYSQL_PASSWORD` und `ADMIN_PASSWORD` wirken nur beim allerersten Start mit leerer
Datenbank. Die App weist darauf hin.

## Starten

```powershell
dotnet run --project Tools.Deployer
```

Der Lösungsordner (`sources`) wird automatisch gefunden, wenn die App darin liegt, sonst einmal ausgewählt und unter
`%AppData%\DartMetricsDeployer\settings.json` gemerkt.

## Veröffentlichen

Eine einzelne `.exe`, die ohne installiertes .NET läuft:

```powershell
dotnet publish Tools.Deployer -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish\deployer
```

Voraussetzungen auf dem Zielrechner: Docker Desktop und der Quellcode (die Images werden lokal gebaut). Für die
Erstinstallation ohne vorhandenes `certs\dartmetrics.pfx` zusätzlich das .NET SDK (`dotnet dev-certs`).

## Aufbau

| Datei | Inhalt |
|---|---|
| `MainForm.cs` | Fenster, Aktionen, Log, Status |
| `Configuration/EnvTemplate.cs` | liest die `.env.example` (Abschnitte, Hilfetexte, Standardwerte) |
| `Configuration/EnvFile.cs` | liest/schreibt die `.env` zeilengenau, setzt Werte mit `$` oder `#` in `'…'` |
| `Configuration/EnvValidator.cs` | Prüfungen wie in den Skripten |
| `Configuration/FieldTexts.cs` | deutsche Beschriftungen und Hinweise |
| `Deployment/DeployTarget.cs` | Umgebungen: `.env`-Datei, Container, Compose-Argumente, Links |
| `Deployment/ProcessRunner.cs` | startet PowerShell (`pwsh`, sonst Windows PowerShell) und `docker`, liefert die Ausgabe zeilenweise |
| `Deployment/DockerStatus.cs` | `docker info` / `docker inspect` |
| `Deployment/RepositoryLocator.cs` | findet den Lösungsordner |

Neue Variable in der `.env.example` → erscheint automatisch im Formular. Für eine schönere Beschriftung in
`FieldTexts.cs` ergänzen; ist sie Pflicht, in `EnvValidator.RequiredKeys` **und** in den Skripten.
