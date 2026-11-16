# Tools.BotTrainer

Entwickler-Werkzeug für die DartBots, ohne Datenbank und ohne Docker. Läuft nie in der API.

```
dotnet run --project Tools.BotTrainer -c Release -- benchmark --legs 2000 --sigmas 6,9,13,20,30
```

## benchmark

Spielt je Streuung (mm, waagerecht; senkrecht ×1,3, 3 % schlechte Darts) Legs ab 501 einmal mit dem Regelwerk und
mit der Tabellen-Zielwahl (`Logic.Bot/Policy`), mit denselben Würfen (gepaarter Vergleich, fester Seed).

| Option | Standard | Bedeutung |
|--------|----------|-----------|
| `--legs` | 2000 | Legs je Streuung |
| `--sigmas` | 6,9,13,20,30 | Streuungen in mm |
| `--tolerances` | 0,0.05,0.2 | Toleranz für Standardwege (Darts) |
| `--samples` | 4000 | Würfe je Ziel für die Trefferverteilung |
| `--straight-out` | – | Straight-Out statt Double-Out |

Ausgabe je Stufe: Darts pro Leg, 3-Dart-Durchschnitt und Differenz zum Regelwerk mit 95-%-Intervall.
