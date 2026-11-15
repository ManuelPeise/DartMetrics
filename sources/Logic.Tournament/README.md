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

## Stand (Phase 3: K.-o./WM)

| Teil | Inhalt |
|---|---|
| `Knockout/KnockoutBracketBuilder` | Baum auf die nächste Zweierpotenz (4–64 Teilnehmer), Standard-Setzung (1 und 2 erst im Finale, 1–4 erst im Halbfinale), Freilose gegen die Besten, Rundennamen vom Finale rückwärts. |
| `Knockout/KnockoutService` (`IKnockoutService`) | Anlegen (optional Setzliste aus einer Liga gleicher Art, 2/4/8 Gesetzte, Standard 4), Teilnehmer, Start: Top N der abgeschlossenen Liga werden gesetzt (fehlende ergänzt), Rest ausgelost, eine Satzlänge je Runde (leer = überall 5), Freilose sofort gewertet. `AdvanceAsync` trägt Sieger in die nächste Runde ein und schließt nach dem Finale ab (idempotent, ohne Rechteprüfung – nur für Server-Abläufe, nicht als API). |
| `TournamentServiceBase` | Gemeinsam für Liga und K.o.: Anlegen, Teilnehmer, Laden mit Rechteprüfung, Termin, Übersicht (`TournamentOverview`). |

## Stand (Phase 4a: Matches spielen)

| Teil | Inhalt |
|---|---|
| `Play/TournamentMatchService` (`ITournamentMatchService`) | Match starten (mindestens ein Mensch; Spielleiter = GameMaster bzw. Besitzer), Profilversion und aktives Zielwahl-Modell der Bots beim Start gespeichert. Jeder Dart über `X01Match`, sofort gespeichert (Leg, Aufnahme, Wurf). Ist danach ein Bot am Wurf, wirft er sofort (`IBotVisitPlayer` mit der Zielwahl aus `ITargetSelectorFactory`, über die Interfaces – kein Verweis auf Logic.Bot). Veralteter `ConcurrencyStamp` → `DbUpdateConcurrencyException` (409). Stand mit Tafel-Daten (Legs, Aufnahmen, Darts, Rest, Average) für Spielleiter und Zuschauer. Matchende: Liga ggf. abschließen, K.o. weiterrücken. |
| `Play/BotMatchWorker` | Hintergrunddienst: startet Bot-gegen-Bot-Matches erst zum Termin der Runde und spielt je Takt eine Aufnahme pro Match (live mitzuverfolgen). Einstellungen `Tournament:BotMatchesEnabled` (true), `Tournament:BotVisitIntervalSeconds` (5). |

## Stand (Phase 4b: API und Seiten)

- `TournamentQueryService` (`ITournamentQueryService`): Turnierliste (offizielle für alle, private nur eigene) mit `CanManage`; Kandidaten (GameMaster: aktive Spieler und Bots, Spieler: er selbst und Bots).
- API `Service.Api/Controllers/Tournament/TournamentController` (Rollen GameMaster, Player; Rechte je Turnier im Service): Liste, Kandidaten, Liga- und K.-o.-Aktionen, Match starten, Dart eingeben, Matchstand. `AdvanceAsync` und der Bot-Takt sind bewusst keine Endpunkte.
- Zustandsfehler (Turnier gestartet, Match läuft nicht, Bot am Wurf …) werfen `BusinessRuleException` (`Shared.Models/Shared`); die Middleware antwortet mit 409 und der Meldung.
- Seiten unter `/player/tournaments` (Web.App.Client): Liste und Anlegen, Detail (Teilnehmer, Start mit Satzlänge, Spielplan/Baum mit Terminen, Tabelle, Match leiten), schlichte Match-Seite mit Eingabe und Live-Ansicht (Nachladen alle 3 s).

Noch offen: eigener Eingabe-Screen mit Tafel und Korrektur (Phase 5), Anmeldung durch Spieler und Live-Verfolgen für Zuschauer im Detail (Phase 6), Elo/XP aus Turnieren und Caller (Phase 7).

## Regeln

- Ein Match = ein Satz, best of 5/7/9/13 Legs; Länge je Runde (`TournamentRounds.BestOfLegs`).
- Spieler 1 = Heim, 2 = Gast; Heim hat den Anwurf im ersten Leg.
- CHECK-Constraints sichern Status, Sieger, Freilos (genau eine Seite), Teilnehmerart und Würfe (gleiche Wurfkodierung wie Training und Bot-Spiele).
- Bot-gegen-Bot-Matches werden nie vorab berechnet; sie starten erst zum Termin der Runde (Phase 4).

## DI

`AddTournamentServices()` in `Web.Core/Bundles/Services.cs` (`ILeagueService`, `IKnockoutService`, `ITournamentMatchService`, Hintergrunddienst `BotMatchWorker`); im Dockerfile per `COPY Logic.Tournament/…` eingetragen. Tests: `Logic.Tournament.Tests`.
