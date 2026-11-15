# Logic.Tournament

Turniere der DartMetrics: Liga (jeder gegen jeden, Hin- und Rückrunde, 8–16 Teilnehmer) und K.-o./WM (Setzliste der besten 4 aus einer Liga, Freilose für Gesetzte). Menschen und Bots spielen gemeinsam; offizielle Turniere legt und leitet ein GameMaster, private ein Spieler. Plan: Doc „Turniere – Plan“.

## Stand (Phase 1: Grundlage)

| Teil | Inhalt |
|---|---|
| Datenmodell (`Data.DartContext/Entities/Tournament`) | `Tournaments`, `TournamentParticipants` (Benutzer oder Bot), `TournamentRounds` (Spieltag bzw. K.-o.-Runde mit Satzlänge und Termin), `TournamentMatches` (Heim/Gast, Bot-Profilversion und Zielwahl-Modell je Seite, Nachfolger im Baum), `TournamentMatchLegs`, `TournamentMatchVisits`, `TournamentMatchThrows` |
| Zugriff | `ITournamentUnitOfWork` über `IUnitOfWork.TournamentUnitOfWork` |
| `Match/X01Match` | X01-Match Dart für Dart ohne Datenbank: Rest, Double-In/-Out bzw. Straight-Out (`X01Rules`), Bust setzt auf den Stand vor der Aufnahme zurück, Leg- und Matchende (`X01SetRules.LegsToWin`), Anwurf wechselt je Leg. `Replay` stellt den Stand aus gespeicherten Darts wieder her (Grundlage für Korrekturen). |

## Stand (Phase 2: Liga)

| Teil | Inhalt |
|---|---|
| `League/LeagueScheduler` | Spielplan nach der Kreismethode: Hinrunde n−1 Spieltage, Rückrunde mit getauschtem Heimrecht; bei ungerader Zahl pro Spieltag einer spielfrei; jeder hat genau n−1 Heimspiele. 8–16 Teilnehmer. |
| `League/LeagueTableCalculator` | Tabelle: 2 Punkte pro Sieg; Gleichstand: Punkte, Leg-Differenz, gewonnene Legs, direkter Vergleich (Mini-Tabelle), 3-Dart-Average, Name. |
| `League/LeagueService` (`ILeagueService`) | Liga anlegen, Teilnehmer hinzufügen/zurückziehen (nur vor dem Start), starten (Spielplan mit zufälliger Reihenfolge, eine Satzlänge für alle Matches), Spieltag terminieren (solange kein Match begonnen hat), Übersicht, Tabelle, Abschluss wenn alle Matches beendet (Sieger = Platz 1). |
| `TournamentAccess` | Rechte im Server: offizielle Turniere verwaltet die Rolle GameMaster, private nur ihr Besitzer (Player); offizielle sieht jeder angemeldete Benutzer, private nur der Besitzer. Teilnehmer: Benutzer mit Rolle Player oder aktive Bots; in privaten Turnieren nur der Besitzer und Bots. |

API-Endpunkte folgen mit dem GameMaster-Bereich (Phase 4).

## Regeln

- Ein Match = ein Satz, best of 5/7/9/13 Legs; Länge je Runde (`TournamentRounds.BestOfLegs`).
- Spieler 1 = Heim, 2 = Gast; Heim hat den Anwurf im ersten Leg.
- CHECK-Constraints sichern Status, Sieger, Freilos (genau eine Seite), Teilnehmerart und Würfe (gleiche Wurfkodierung wie Training und Bot-Spiele).
- Bot-gegen-Bot-Matches werden nie vorab berechnet; sie starten erst zum Termin der Runde (Phase 4).

## DI

`AddTournamentServices()` in `Web.Core/Bundles/Services.cs` (`ILeagueService`); im Dockerfile per `COPY Logic.Tournament/…` eingetragen. Tests: `Logic.Tournament.Tests`.
