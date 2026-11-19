# Logic.Bot.Tests

xUnit-Tests für die datenbankfreie Bot-Logik.

| Bereich | Was geprüft wird |
|---|---|
| `Simulation/DartBoardGeometryTests` | Jeder Zielpunkt löst auf sein eigenes Feld auf, Ausrichtung der Scheibe, Miss außerhalb |
| `Simulation/ThrowSimulatorTests` | Streuung je Precision (Grenzen, monoton), ohne Streuung exakt aufs Ziel, bessere Precision trifft öfter |
| `Tactics/RuleBasedTargetSelectorTests` | T20 weit weg, Double-In-Eröffnung, Ein-Dart-Finishes, Straight-Out, nie ein Ziel, das sicher bustet |
| `Play/BotVisitPlayerTests` | 180 aus 501, Checkouts 40/100/170, Double-In, ungültiger Reststand |
| `Learning/DartBotLearningServiceLevelTests` | XP-Schwellen und Level, Deckelung auf MaxLevel |
| `Rules/X01RulesTests` | Bust/Checkout für Double-/Straight-Out, CountedScore bei Double-In |
| `Rules/X01VisitPreviewTests` | Browser-Vorschau einer Aufnahme stimmt mit `X01Rules` überein (alle Darts × 2, Reste 1–80, alle Modi) |
| `Rules/AroundTheClockRulesTests` | Around the Clock: Treffer je Ziel (Bull zählt für „25“), nächstes Ziel |
| `Rules/CheckoutAdvisorTests` | Checkout-Empfehlung für Menschen: alle Reste 2–170 (gültiger Weg, wenigste Darts, kein Zwischenrest < 2), unmögliche Reste (159, 162, 163, 165, 166, 168, 169), Standardwege (170, 160, 100, 98, 60, 40, 32, 50), Straight-Out, Kopien statt Cache-Objekten |

`Helpers/FixedRandomSource` liefert immer 0.25. Box-Muller ergibt damit 0, also keine Streuung: der Bot trifft jeden Zielpunkt, die Tests sind deterministisch.

Ausführen: `dotnet test Logic.Bot.Tests`

Nicht abgedeckt (braucht Datenbank, gehört in Integrationstests): `DartBotGameService`, `ApplyGameResultAsync`, `MergeConcurrentBotUpdatesAsync`.
