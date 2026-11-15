# Sicherheitsrichtlinie

## Sicherheitslücke melden

Bitte melde Sicherheitslücken **nicht** als öffentliches Issue, sondern vertraulich über
**GitHub → Security → „Report a vulnerability“** (Private Vulnerability Reporting) in diesem Repository.

Hilfreich sind:

- betroffene Komponente (z. B. Web-App, API, Docker-Konfiguration) und Version bzw. Commit,
- Schritte zum Nachvollziehen,
- mögliche Auswirkungen.

Du bekommst in der Regel innerhalb von 7 Tagen eine erste Rückmeldung. Bestätigte Lücken werden behoben und
nach dem Fix in den Release-Notes bzw. als GitHub Security Advisory veröffentlicht.

## Unterstützte Versionen

Sicherheitsupdates gibt es nur für den aktuellen Stand des Branches `master`.

## Hinweis für Betreiber

DartMetrics ist Open Source (MIT-Lizenz) und wird ohne Gewähr bereitgestellt. Wer eine eigene Instanz betreibt,
ist für deren sicheren Betrieb selbst verantwortlich, unter anderem für:

- sichere Werte in der `.env` (Passwörter, `JWT_KEY`) und regelmäßige Updates (`scripts/update-containers.ps1` / `.sh`),
- die Produktions-Konfiguration (`COMPOSE_FILE=docker-compose.yml`: keine offenen Datenbank- und API-Ports),
- HTTPS mit einem gültigen Zertifikat, wenn die Instanz außerhalb des Heimnetzes erreichbar ist,
- Backups (`scripts/backup-db.sh`),
- Impressum und Datenschutzerklärung (Vorlagen unter `/impressum` und `/datenschutz`, Angaben über `LEGAL_*` in der `.env`).
