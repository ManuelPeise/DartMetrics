# Logic.Bot.Tests

xUnit-Tests für die datenbankfreie Bot-Logik.

| Bereich | Was geprüft wird |
|---|---|
| `Simulation/DartBoardGeometryTests` | Jeder Zielpunkt löst auf sein eigenes Feld auf, Ausrichtung der Scheibe, Miss außerhalb |
| `Simulation/ThrowSimulatorTests` | Streuung je Precision (Grenzen, monoton), ohne Streuung exakt aufs Ziel, bessere Precision trifft öfter |
| `Tactics/RuleBasedTargetSelectorTests` | T20 weit weg, Double-In-Eröffnung, Ein-Dart-Finishes, Master-Out, nie ein Ziel, das sicher bustet |
| `Play/BotVisitPlayerTests` | 180 aus 501, Checkouts 40/100/170, Double-In, ungültiger Reststand |
| `Learning/DartBotLearningServiceLevelTests` | XP-Schwellen und Level, Deckelung auf MaxLevel |
| `Rules/X01RulesTests` | Bust/Checkout für Double-/Master-Out, CountedScore bei Double-In |

`Helpers/FixedRandomSource` liefert immer 0.25. Box-Muller ergibt damit 0, also keine Streuung: der Bot trifft jeden Zielpunkt, die Tests sind deterministisch.

Ausführen: `dotnet test Logic.Bot.Tests`

Nicht abgedeckt (braucht Datenbank, gehört in Integrationstests): `DartBotGameService`, `ApplyGameResultAsync`, `MergeConcurrentBotUpdatesAsync`.
