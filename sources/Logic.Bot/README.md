# Logic.Bot

Lernfähige DartBots: Wurfsimulation, Zielwahl, Spiele gegen Menschen, Lernschritt, Verwaltung.

## Struktur

```
Simulation/DartBoardGeometry       Scheibe in mm (Ursprung Bull-Mitte, 20 oben): Zielpunkt ↔ Feld
Simulation/ThrowSimulator          Treffer = Ziel + Abweichung + Streuung (senkrecht stärker), schlechte Darts, Ablenkung durch steckende Darts
Simulation/LegRolloutSimulator     Ein Leg zu Ende spielen (Prüfstand)
Policy/ThrowOutcomeModel           Trefferverteilung je Ziel für ein Wurfprofil
Policy/PolicyTable                 Exakte Zielwahl (dynamische Programmierung): erwartete Darts je Zustand
Policy/PolicyTableTargetSelector   Zielwahl aus der Tabelle, Standardwege bevorzugt, Taktik-Fehler
Tactics/RuleBasedTargetSelector    Zielwahl ohne ML: Checkout-Wege (X01Rules), T20, sicheres Setup
Play/BotVisitPlayer                Eine Aufnahme (bis 3 Darts) inkl. Bust/Checkout
Games/DartBotGameService           Spiel Mensch gegen Bot (Start, Aufnahme, Legs, Ende, Abbruch, Bot-Liste)
Learning/DartBotLearningService    Nach Spielende: XP, Elo (Bot und Spieler), Level-Aufstieg mit neuer Profilversion
Administration/DartBotAdministration  Bots anlegen, (de)aktivieren, auflisten (nur Admin)
DI/BotServiceRegistration          AddBotServices()
```

## Fachliche Regeln

- Spieler 1 = Mensch, Spieler 2 = Bot. Best of 1–11 Legs, Anwurf wechselt je Leg.
- Satz je Spiel: Training immer Best of 5, Match 5/7/9/13 Legs (`X01SetRules`), Ende bei Mehrheit. Turnier wird bis zum Turnier-Feature abgelehnt.
- X01-Varianten je Spiel: Straight-/Double-In, Double-/Straight-Out (bei Straight-Out finisht der Bot auch auf Single, höchstes Finish 180). Bei Double-In zielt der Bot bis zur Eröffnung auf D20; Checkout-Wege richten sich nach dem Out-Modus.
- Ein Spiel speichert die Profilversion vom Start.
- Lernen nur bei `Finished` und menschlichem Gegner, genau einmal (`LearningProcessedAt`).
- Level aus Gesamt-XP (`LevelXpBase × n^LevelXpExponent`), Fähigkeiten steigen linear bis `SkillAtMaxLevel`.
- Alle Werte konfigurierbar über `DartBotOptions` (Abschnitt `"DartBot"`).
- Zufall über `IRandomSource` (für reproduzierbare Tests austauschbar).

## Nebenläufigkeit

`DartBotEntity` hat ein `ConcurrencyStamp` (Guid, Concurrency-Token; neu gesetzt im `UnitOfWork` bei jedem Insert/Update).
Enden zwei Spiele gegen denselben Bot gleichzeitig, wirft das zweite Speichern eine `DbUpdateConcurrencyException`.
`DartBotGameService.AddUserVisitAsync` ruft dann `DartBotLearningService.MergeConcurrentBotUpdatesAsync` auf:
die eigenen XP- und Elo-Deltas werden auf den aktuellen Datenbankstand gerechnet, das Level wird nie zurückgestuft, danach wird einmal erneut gespeichert.
Betrifft der Konflikt andere Entitäten oder scheitert auch der zweite Versuch, liefert die API 409 (`GlobalExceptionHandlerMiddleware`).

## Turniere

`ApplyTournamentResultAsync` wertet beendete Matches offizieller Turniere: Elo für beide Seiten (auch Bot gegen Bot), XP und Level-Aufstieg für Bots mit denselben Regeln wie bei Bot-Spielen (`ApplyLevelUp` gemeinsam genutzt). Aufgerufen von Logic.Tournament über `IDartBotLearningService`.

## Zielwahl ohne ML

ML.NET wurde entfernt: das Modell sparte nur ~0,5 Darts pro Leg und brauchte Datensätze, Training und Schemas.
Stattdessen: Standard-Checkout-Wege und eine exakt berechnete Tabelle je Wurfprofil (`Policy/`), siehe Tools.BotTrainer `benchmark`.
