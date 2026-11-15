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
- Mit aktivem Modell: `HybridTargetSelector` hinter `ITargetSelector` (siehe ML, Phase 5).

## Nebenläufigkeit

`DartBotEntity` hat ein `ConcurrencyStamp` (Guid, Concurrency-Token; neu gesetzt im `UnitOfWork` bei jedem Insert/Update).
Enden zwei Spiele gegen denselben Bot gleichzeitig, wirft das zweite Speichern eine `DbUpdateConcurrencyException`.
`DartBotGameService.AddUserVisitAsync` ruft dann `DartBotLearningService.MergeConcurrentBotUpdatesAsync` auf:
die eigenen XP- und Elo-Deltas werden auf den aktuellen Datenbankstand gerechnet, das Level wird nie zurückgestuft, danach wird einmal erneut gespeichert.
Betrifft der Konflikt andere Entitäten oder scheitert auch der zweite Versuch, liefert die API 409 (`GlobalExceptionHandlerMiddleware`).

## Turniere

`ApplyTournamentResultAsync` wertet beendete Matches offizieller Turniere: Elo für beide Seiten (auch Bot gegen Bot), XP und Level-Aufstieg für Bots mit denselben Regeln wie bei Bot-Spielen (`ApplyLevelUp` gemeinsam genutzt). Aufgerufen von Logic.Tournament über `IDartBotLearningService`.

## ML (Phase 1: Kandidaten und Rollouts)

Grundlage des Zielwahl-Modells: Datensätze für das Training (Tools.BotTrainer). Reine Logik ohne Datenbank. Im Bot-Spiel wird das trainierte Modell über die Hybrid-Zielwahl genutzt (Phase 5); ohne aktives Modell spielen die Bots regelbasiert.

| Klasse | Aufgabe |
|---|---|
| `Ml/CandidateGenerator` | sinnvolle Ziele einer Situation (S/D/T 1–20, 25, Bull) ohne sichere Busts; vor der Double-In-Eröffnung nur Doubles |
| `Ml/CandidateFeatureBuilder` | Merkmale je Kandidat (`TargetCandidateFeatures`, FeatureSchemaVersion 2) |
| `Ml/LegRolloutSimulator` | spielt ab dem Kandidaten-Dart das Leg zu Ende (Regelstrategie, `ThrowSimulator`, `X01Rules`), zählt die Darts |
| `Ml/CandidateEvaluator` | N Rollouts je Kandidat (Standard 50, max. 60 Darts), Mittelwert = `ExpectedDartsToFinish`, Abstand zum besten Kandidaten = Label `RelativeDarts` (Schema 2). Gemeinsamer Zufall: Rollout i hat für alle Kandidaten einer Situation dieselben Würfe; Strategie mit eigenem Strom |
| `Ml/RolloutOptions` | `RolloutsPerCandidate`, `MaxDartsPerLeg`, `Seed` |

Situation: `Shared.Models/Bot/Ml/BotSituation` (Rest, Darts in der Aufnahme, Gegnerstand, Profil, In-/Out-Modus, eröffnet, Stand zu Beginn der Aufnahme).

## ML (Phase 5: Modell im Bot-Spiel)

| Klasse | Aufgabe |
|---|---|
| `Ml/TargetCandidateRow` | Eingabezeile des ML.NET-Modells (aus `TargetCandidateFeatures`), `DartsPrediction` als Ausgabe |
| `Ml/LoadedTargetModel` | lädt die ZIP nach SHA-256-Prüfung, Pool von `PredictionEngine`s (threadsicher) |
| `Ml/HybridTargetSelector` | nimmt das Regel-Ziel, ersetzt es nur, wenn das Modell einen Kandidaten um mehr als `Margin` Darts besser bewertet |
| `Ml/TargetSelectorFactory` | Singleton, `ITargetSelectorFactory`: Modell je Id einmal geladen; fehlt die Datei, passt SHA/Schema nicht oder ist ML aus → Regelwerk |

Ablauf: `StartGameAsync` merkt sich das aktive Modell (`DartBotActiveModels`) in `DartBotGamePlayer.TargetModelId`; `PlayBotVisit` holt die Zielwahl über die Factory. Laufende Spiele behalten ihr Modell, auch nach einem Wechsel.

Einstellungen `DartBot:Ml`: `Enabled` (true), `Path` (Ordner der ML-Daten, Container `/data/ml`), `Margin` (0,3 – im Vergleich auf allen Skill-Stufen signifikant besser als das Regelwerk). Aktivieren: Admin-Seite `/admin/models`, `Tools.BotTrainer activate --model <id>` oder komplett automatisch `deploy/train-model.ps1` / `.sh` (siehe Tools.BotTrainer).

## ML (Phase 6: Admin-Seite)

`Administration/DartBotModelAdministration` (`IDartBotModelAdministration`, nur Admin, Prüfung im Server): listet die Zielwahl-Modelle mit Training (Datensatz, Trefferquote, Regret) und letztem Vergleich aus `MetricsJson`, prüft jede Datei mit `Ml/ModelFileCheck` (Pfad im ML-Ordner, vorhanden, Schema, SHA-256) und setzt bzw. entfernt das aktive Modell. Aktivieren geht nur mit gültiger Datei; `ActivatedBy` = E-Mail des Admins. API: `DartBotModelAdministration/GetDartBotModels`, `ActivateDartBotModel?modelId=`, `DeactivateDartBotModel`.
