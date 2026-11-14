# Logic.Bot

Lernfähige DartBots: Wurfsimulation, Zielwahl, Spiele gegen Menschen, Lernschritt, Verwaltung.

## Struktur

```
Simulation/DartBoardGeometry       Scheibe in mm (Ursprung Bull-Mitte, 20 oben): Zielpunkt ↔ Feld
Simulation/ThrowSimulator          Treffer = Ziel + 2D-Normalverteilung (Precision, Consistency, Pressure)
Tactics/RuleBasedTargetSelector    Zielwahl ohne ML: Checkout-Wege (X01Rules), T20, sicheres Setup
Play/BotVisitPlayer                Eine Aufnahme (bis 3 Darts) inkl. Bust/Checkout
Games/DartBotGameService           Spiel Mensch gegen Bot (Start, Aufnahme, Legs, Ende, Abbruch, Bot-Liste)
Learning/DartBotLearningService    Nach Spielende: XP, Elo, Level-Aufstieg mit neuer Profilversion
Administration/DartBotAdministration  Bots anlegen, (de)aktivieren, auflisten (nur Admin)
DI/BotServiceRegistration          AddBotServices()
```

## Fachliche Regeln

- Spieler 1 = Mensch, Spieler 2 = Bot. Best of 1–11 Legs, Anwurf wechselt je Leg.
- X01-Varianten je Spiel: Straight-/Double-In, Double-/Master-Out. Bei Double-In zielt der Bot bis zur Eröffnung auf D20; Checkout-Wege richten sich nach dem Out-Modus.
- Ein Spiel speichert Profilversion und aktive Modelle vom Start.
- Lernen nur bei `Finished` und menschlichem Gegner, genau einmal (`LearningProcessedAt`).
- Level aus Gesamt-XP (`LevelXpBase × n^LevelXpExponent`), Fähigkeiten steigen linear bis `SkillAtMaxLevel`.
- Alle Werte konfigurierbar über `DartBotOptions` (Abschnitt `"DartBot"`).
- Zufall über `IRandomSource` (für reproduzierbare Tests austauschbar).
- Später: `MlTargetSelector` hinter `ITargetSelector`.
