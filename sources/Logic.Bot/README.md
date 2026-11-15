# Logic.Bot

Lernfähige DartBots: Wurfsimulation, Zielwahl, Spiele gegen Menschen, Lernschritt, Verwaltung.

## Struktur

```
Simulation/DartBoardGeometry       Scheibe in mm (Ursprung Bull-Mitte, 20 oben): Zielpunkt ↔ Feld
Simulation/ThrowSimulator          Treffer = Ziel + 2D-Normalverteilung (Precision, Consistency, Pressure)
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
- Ein Spiel speichert Profilversion und aktive Modelle vom Start.
- Lernen nur bei `Finished` und menschlichem Gegner, genau einmal (`LearningProcessedAt`).
- Level aus Gesamt-XP (`LevelXpBase × n^LevelXpExponent`), Fähigkeiten steigen linear bis `SkillAtMaxLevel`.
- Alle Werte konfigurierbar über `DartBotOptions` (Abschnitt `"DartBot"`).
- Zufall über `IRandomSource` (für reproduzierbare Tests austauschbar).
- Später: `MlTargetSelector` hinter `ITargetSelector`.

## Nebenläufigkeit

`DartBotEntity` hat ein `ConcurrencyStamp` (Guid, Concurrency-Token; neu gesetzt im `UnitOfWork` bei jedem Insert/Update).
Enden zwei Spiele gegen denselben Bot gleichzeitig, wirft das zweite Speichern eine `DbUpdateConcurrencyException`.
`DartBotGameService.AddUserVisitAsync` ruft dann `DartBotLearningService.MergeConcurrentBotUpdatesAsync` auf:
die eigenen XP- und Elo-Deltas werden auf den aktuellen Datenbankstand gerechnet, das Level wird nie zurückgestuft, danach wird einmal erneut gespeichert.
Betrifft der Konflikt andere Entitäten oder scheitert auch der zweite Versuch, liefert die API 409 (`GlobalExceptionHandlerMiddleware`).

## ML (Phase 1: Kandidaten und Rollouts)

Grundlage für das spätere Zielwahl-Modell (Plan-Doc „ML.NET Phase 1 – Kandidaten und Rollouts“). Reine Logik ohne Datenbank, nicht in der API registriert; Bots spielen weiter regelbasiert.

| Klasse | Aufgabe |
|---|---|
| `Ml/CandidateGenerator` | sinnvolle Ziele einer Situation (S/D/T 1–20, 25, Bull) ohne sichere Busts; vor der Double-In-Eröffnung nur Doubles |
| `Ml/CandidateFeatureBuilder` | Merkmale je Kandidat (`TargetCandidateFeatures`, FeatureSchemaVersion 1) |
| `Ml/LegRolloutSimulator` | spielt ab dem Kandidaten-Dart das Leg zu Ende (Regelstrategie, `ThrowSimulator`, `X01Rules`), zählt die Darts |
| `Ml/CandidateEvaluator` | N Rollouts je Kandidat (Standard 50, max. 60 Darts), Mittelwert = Label `ExpectedDartsToFinish`; parallel, reproduzierbar über Seed |
| `Ml/RolloutOptions` | `RolloutsPerCandidate`, `MaxDartsPerLeg`, `Seed` |

Situation: `Shared.Models/Bot/Ml/BotSituation` (Rest, Darts in der Aufnahme, Gegnerstand, Profil, In-/Out-Modus, eröffnet, Stand zu Beginn der Aufnahme).
