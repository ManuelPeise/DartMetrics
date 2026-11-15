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
- **Docker Compose** für API, Datenbank und Mailpit.

Architektur, Projekte und Service-Verzeichnis: **[ARCHITECTURE.md](ARCHITECTURE.md)** · Entwicklungsregeln: **[CLAUDE.md](CLAUDE.md)**

## Schnellstart

```
docker compose up -d                  # MySQL, Mailpit, API  → http://localhost:8080/swagger
dotnet run --project Web.App/Web.App  # Web-App
```

- Geheimnisse (Datenbank, JWT, Admin) in `.env` im Ordner `sources` (nicht im Repository).
- Mails der Entwicklungsumgebung: http://localhost:8025
- API aktualisieren: `scripts/update-containers.ps1` (Windows) bzw. `scripts/update-containers.sh`.
