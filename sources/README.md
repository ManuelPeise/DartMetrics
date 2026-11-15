# DartMetrics

**Jeder Wurf zählt.** DartMetrics ist eine Web-Anwendung für Steel- und Softdart-Spieler: Spiele und Trainings werden Wurf für Wurf erfasst, ausgewertet und über die Zeit verfolgt. Dazu kommen virtuelle Gegner – DartBots –, die mit jedem Spiel gegen Menschen dazulernen.

## Was die Anwendung kann

### Für Spieler
- **Konto:** Registrierung mit E-Mail-Bestätigung, Login, Profil, Passwort ändern.
- **Trainings**, jeder Dart einzeln erfasst:
  - *Segment-Training* – ein Zielfeld über mehrere Aufnahmen treffen.
  - *Checkout-Training* – Finishes von einem Startwert üben.
  - *X01* – 301 oder 501 mit Double-Out allein spielen.
  - *Around the Clock* – die Segmente der Reihe nach treffen.
- **Statistiken:** Averages, Checkout- und Trefferquoten, Siege gegen Bots und Verlauf von Average und Elo – für 30 Tage, 90 Tage oder gesamt.
- **Spiele gegen DartBots:** 301/501, Best of 1–11 Legs. Der Bot antwortet sofort mit seiner Aufnahme und zeigt, worauf er gezielt hat.

### DartBots
- Jeder Bot startet als **Anfänger (Level 0)** und entwickelt sich bis zum Profi (Level 20).
- Nach jedem abgeschlossenen Spiel gegen einen Menschen erhält der Bot **Erfahrungspunkte und eine Elo-Wertung**; ein Level-Aufstieg verbessert Präzision, Konstanz, Taktik und Druckresistenz.
- Würfe werden realistisch simuliert (Streuung um das Ziel, Formschwankungen, Druck beim Checkout).
- Die Zielwahl ist zunächst regelbasiert (Checkout-Wege, Setup-Würfe); später übernehmen gemeinsam trainierte **ML.NET-Modelle**.

### Für Administratoren (`/admin`)
- **Dashboard** mit Benutzern, Bots, Systemstatus und letzten Aktivitäten.
- **Benutzerverwaltung:** Rollen vergeben (Player/GameMaster), Konten sperren oder anonymisiert löschen. Admin-Konten und das eigene Konto sind geschützt.
- **DartBot-Verwaltung:** Bots anlegen, aktivieren, deaktivieren, Entwicklung verfolgen.
- **System-Logs:** Fehler, Warnungen und Hinweise durchsuchen und bereinigen.

## Rollen

| Rolle | Zugriff |
|---|---|
| Player | Eigene Trainings, Statistiken, Spiele gegen Bots |
| GameMaster | Erweiterte Spielfunktionen |
| Admin | Administrationsbereich |

Jeder Benutzer hat genau eine Rolle.

## Technik

- **.NET 10**, ASP.NET Core Web API, **Blazor** (WebAssembly für Spieler, Interactive Server für die Administration), Backend-for-Frontend mit Cookie-Anmeldung und JWT zur API.
- **MySQL 8.4** mit Entity Framework Core (Oracle-Provider), Migrationen beim Start.
- **ML.NET** für die Bot-Zielwahl (in Vorbereitung).
- **Docker Compose** für Web-App, API, Datenbank und Mailpit.

Architektur, Projekte und Service-Verzeichnis: **[ARCHITECTURE.md](ARCHITECTURE.md)** · Entwicklungsregeln: **[CLAUDE.md](CLAUDE.md)**

## Schnellstart

```
cp .env.example .env                  # Platzhalter ersetzen (Passwörter, JWT_KEY, CERT_PASSWORD, LEGAL_*)
./deploy/download-fonts.ps1          # Schriften einmalig lokal ablegen (kein Google Fonts, DSGVO)
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
| `deploy/deploy` | Erstinstallation bzw. kompletter Neuaufbau: prüft `.env`, Zertifikat und Schriften (legt fehlende an), baut alle Images, startet, wartet auf API/Web-App und trainiert ein ML-Modell, falls keins aktiv ist. `-Production` / `--production` ohne offene DB/API-Ports und ohne Mailpit. |
| `deploy/update-containers` | Code-Update: Datenbank-Backup, API/Web-App neu bauen und ersetzen. |
| `deploy/train-model` | DartBot-Zielwahl-Modell in Docker erzeugen → trainieren → mit dem Regelwerk vergleichen → aktivieren (nur wenn nicht schlechter). Ohne `-Force` / `--force` nur, wenn noch keins aktiv ist. |
| `deploy/backup-db` (`.sh`) | Tägliches Datenbank-Backup per Cron, mit Rotation. |
| `deploy/download-fonts` | Schriften einmalig lokal ablegen. |

Ohne aktives Modell spielen die Bots regelbasiert; das funktioniert, ist aber schwächer. Das Training dauert mit Standardwerten grob 10–30 Minuten auf einem PC, auf einem Raspberry Pi deutlich länger (kleinere Werte: `-Situations 300 -Rollouts 30 -Legs 500`).

Einzelne Trainer-Befehle: `docker compose --profile tools run --rm bottrainer <generate|train|compare|activate|deactivate> …` (siehe `Tools.BotTrainer/README.md`).

## Eigene Instanz betreiben

DartMetrics ist Open Source (MIT-Lizenz) und wird ohne Gewähr bereitgestellt. **Wer eine Instanz betreibt, ist dafür selbst verantwortlich**, rechtlich (Impressum, Datenschutz, DSGVO) und technisch (Updates, Backups, HTTPS).

- Impressum und Datenschutzerklärung (`/impressum`, `/datenschutz`) sind **Vorlagen**. Sie beschreiben, was die App ab Werk speichert, ersetzen aber keine rechtliche Prüfung. Bitte prüfen und an die eigene Instanz anpassen, vor allem bei geändertem Code, eigenem Mail-Anbieter oder öffentlichem Betrieb.
- Die Angaben zum Betreiber kommen aus `LEGAL_*` in der `.env` (siehe `.env.example`). Im Repository stehen keine persönlichen Daten.
- Sicherheitslücken bitte vertraulich melden: siehe [SECURITY.md](../SECURITY.md).
