# Logic.Training

Trainingsmodi des Spielers. Jede Trainingseinheit ist eine `TrainingSessionEntity` mit typabhängigen Detaildaten.

| Klasse | Modus |
|---|---|
| `SegmentTraining` | Ein Zielfeld über mehrere Aufnahmen treffen |
| `CheckoutTraining` | Checkouts von einem Startwert |
| `X01TrainingGame` | 301/501 allein spielen (Straight-/Double-In, Double-/Master-Out) |
| `AroundTheClockTraining` | Segmente der Reihe nach |

- `TrainingBase`: aktueller Benutzer, Laden eigener Trainings (`LoadSegmentTraining`, `LoadCheckoutTraining`, `LoadX01Training`, `LoadAroundTheClockTraining`), Wurfprüfung.
- **`TrainingId` ist immer die Id der `TrainingSession`** (Rückgabe von `Start…`). Geladen wird über `TrainingSessionId` und den Benutzer der Session.
- Jeder `AddThrow…` liefert ein Ergebnis je Dart (Rest, Bust/Checkout, Treffer, Ende), damit die Oberfläche keine Dart-Regeln nachbauen muss.
- Darts strikt der Reihe nach (Aufnahme/Versuch und Dart-Nummer werden geprüft).
- Checkout-Training: Versuch = bis zu 3 Darts auf den Zielwert, endet mit Checkout, Bust oder dem 3. Dart; immer Double-Out.
- Regeln aus `Logic.Shared/Rules`.
- DI: `AddTrainingServices()` registriert alle vier Services.

## Fortsetzen nach Reload

Jedes Training hat `GetRunningTrainingAsync()` (API: `GetRunning<Art>Training`, 204 wenn nichts offen ist).
Geliefert wird die neueste offene Session dieser Art des Benutzers mit allem, was die Seite zum Weiterspielen braucht:
Position des nächsten Darts, bereits geworfene Darts der angefangenen Aufnahme bzw. des Versuchs, Reststand/Ziel, Treffer, Busts, Checkout-Wege.
Die Werte werden aus den gespeicherten Würfen nach denselben Regeln wie in `AddThrowAsync` berechnet.
