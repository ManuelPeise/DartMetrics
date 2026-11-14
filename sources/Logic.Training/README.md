# Logic.Training

Trainingsmodi des Spielers. Jede Trainingseinheit ist eine `TrainingSessionEntity` mit typabhängigen Detaildaten.

| Klasse | Modus |
|---|---|
| `SegmentTraining` | Ein Zielfeld über mehrere Aufnahmen treffen |
| `CheckoutTraining` | Checkouts von einem Startwert |
| `X01TrainingGame` | 301/501 allein spielen |
| `AroundTheClockTraining` | Segmente der Reihe nach |

- `TrainingBase`: aktueller Benutzer, Laden eigener Trainings, Wurfprüfung.
- Regeln aus `Logic.Shared/Rules`.
- DI: `AddTrainingServices()` registriert alle vier Services.
