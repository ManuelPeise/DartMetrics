# Logic.Tournament.Tests

Unit-Tests der Turnier-Logik ohne Datenbank (xUnit). - `X01MatchTests`: Aufnahme, Bust, Double-/Straight-Out, Double-In, Legwechsel, Matchende bei Mehrheit, Replay, ungültige Darts.
- `LeagueSchedulerTests`: Spieltage, jede Paarung zweimal mit getauschtem Heimrecht, einmal pro Spieltag, Spielfrei nur bei ungerader Zahl, ausgeglichenes Heimrecht, Grenzen 8–16.
- `LeagueTableCalculatorTests`: Punkte, Leg-Differenz, gewonnene Legs, direkter Vergleich vor Average, kein Unentschieden.

Der Ablauf gegen MySQL steht in `Logic.Integration.Tests/Tournament/LeagueFlowTests`.

`dotnet test Logic.Tournament.Tests`
