# Logic.Statistics.Tests

xUnit-Tests für den Rechenkern der Statistik (`Logic.Statistics/Performance`), ohne Datenbank.

`LegBuilder` baut Legs wie die echte Wertung (Reststand je Dart, Bust, Checkout) aus Kurzschrift: `V("T20", "T19", "D12")`.

| Test | Geprüft |
|---|---|
| Neun-Darter | Average und First 9 = 167, zwei 180er, 140+ kumuliert, 180er pro 100 Legs, 1 Doppel-Versuch = 100 %, Highest Checkout 141, bestes Leg 9 Darts |
| Missed Doubles | Rest 40 → S20, S10, Miss, dann D5: 4 Versuche, 1 Treffer, 25 % |
| Bust | 0 Punkte, Darts zählen im Average |
| Straight-Out | keine Doppel-Versuche, Checkout trotzdem als Highest Checkout |
| First 9 | nur die ersten drei Aufnahmen |
| Unterbrochenes Leg | zählt im Average, nicht als Leg |
| Siegquote, Siegesserie | Reihenfolge nach Datum |
| Keine Daten | `null` statt 0 |
| Vorzeitraum, letzte 10 Matches, Bestwerte, Score-Verteilung, Hinweise | Vergleiche, Mindestmengen, Bänder, Schwellen |

Ausführen: `dotnet test Logic.Statistics.Tests`
