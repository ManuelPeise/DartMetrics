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
| `Tournament/KnockoutFlowTests` | Baum mit Freilosen, Weiterrücken bis zum Sieger (idempotent), Setzliste Top 4 aus abgeschlossener Liga, Quell-Liga muss fertig sein, Satzlängen je Runde |
| `Tournament/TournamentMatchFlowTests` | Mensch gegen Bot: GameMaster gibt Darts ein, Bot wirft sofort, Ergebnis in der Tabelle; Korrektur nimmt Dart und Bot-Antwort zurück; Spieler darf nicht eingeben; veralteter Stand → Konflikt. Bot gegen Bot: erst ab Termin, eine Aufnahme je Takt, bis zum Ende |
| `Tournament/TournamentRegistrationTests` | Anmeldung öffnen, Spieler meldet sich an (wartet), GameMaster bestätigt, Abmelden und erneut anmelden; private Turniere ohne Anmeldung |
| `Account/AccountRecoveryTests` | Passwort-Reset (einmal nutzbar, überall abgemeldet, Spam-Sperre, ungültiges Token, unbekannte Adresse), Bestätigungsmail nur für unbestätigte Konten, Konto selbst löschen (falsches Passwort, Anonymisierung, Liga-Matches kampflos), Admin-Löschung meldet von nicht gestarteten Turnieren ab; Passwort ändern hebt das Einmal-Passwort auf |
| `Account/LoginAndRegistrationTests` | Registrierung → Bestätigung → Login, schwaches/abweichendes Passwort, bereits bestätigte Adresse, manipuliertes Token; fehlgeschlagener Login ohne Log-Eintrag; inaktives Konto; Refresh-Token-Rotation (altes Token wertlos); Löschfristen (nur alte unbestätigte Konten, Cascade der Anmeldedaten) |
| `Profile/UserProfileServiceTests` | Passwort ändern (richtig, falsch, zu kurz, gleich), Profilbild (PNG ok, Nicht-Bild abgelehnt, Löschen), Namensvalidierung |

## Regeln

- Neue Tests: `[Collection(MySqlCollection.Name)]`, Host in `InitializeAsync` erzeugen, Daten über die Services anlegen (nicht direkt in die DB), Prüfungen gern über `DbAsync`.
- Jede Aktion in einem eigenen Scope (`RunAsync`), damit nichts aus dem Change Tracker „mitgeschleppt“ wird.
