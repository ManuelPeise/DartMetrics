# Logic.Integration.Tests

Integrationstests gegen eine echte **MySQL 8.4** (Testcontainers) – gleiche Datenbank wie in `docker-compose.yml`.
Prüfen, was Unit-Tests nicht können: SQL-Übersetzung, Constraints, Migrationen, Concurrency-Token, Abläufe über mehrere Anfragen.

**Voraussetzung:** Docker läuft. Ausführen: `dotnet test Logic.Integration.Tests` (bzw. `dotnet test DartMetrics.slnx`, auch über `update-containers -Test`).

## Aufbau

| Datei | Zweck |
|---|---|
| `Infrastructure/MySqlFixture` | Ein Container pro Testlauf; jede Testklasse/-methode bekommt eine eigene, frische Datenbank |
| `Infrastructure/TestHost` | Echte DI wie in Web.Core (`AddSharedServices`, `AddBotServices`, …), wendet die Migrationen an; jeder Scope = eine Anfrage |
| `Infrastructure/TestClaimsHelper` | Angemeldeter Benutzer per `SignIn(userId, role)` |
| `Infrastructure/FixedRandomSource` | Bot ohne Streuung → deterministische Spiele |

## Tests

| Bereich | Geprüft |
|---|---|
| `Bot/BotGameFlowTests` | Ganzes Spiel bis zum Sieg des Bots; XP und Elo (Bot und Spieler) genau einmal; Fortsetzen; Abbruch ohne Elo; Elo-Verlauf |
| `Bot/BotConcurrencyTests` | Zwei Anfragen ändern denselben Bot → Konflikt, Merge, keine XP verloren |
| `Training/TrainingResumeTests` | Fortsetzen aller vier Trainings (Position, Rest, Treffer, Double-In), beendete werden nicht angeboten |
| `Statistics/PlayerStatisticsTests` | Nur beendete Trainings zählen; Average stimmt mit der Ergebnisseite überein; leerer Zeitraum |
| `Tournament/LeagueFlowTests` | Liga mit Spielern und Bots starten (Spielplan Hin- und Rückrunde, Spielfrei), Spieltag terminieren; Rechte GameMaster/Player, private Liga nur Besitzer und Bots; Grenzen; CHECK auf Teilnehmerart |
| `Profile/UserProfileServiceTests` | Passwort ändern (richtig, falsch, zu kurz, gleich), Profilbild (PNG ok, Nicht-Bild abgelehnt, Löschen), Namensvalidierung |

## Regeln

- Neue Tests: `[Collection(MySqlCollection.Name)]`, Host in `InitializeAsync` erzeugen, Daten über die Services anlegen (nicht direkt in die DB), Prüfungen gern über `DbAsync`.
- Jede Aktion in einem eigenen Scope (`RunAsync`), damit nichts aus dem Change Tracker „mitgeschleppt“ wird.
