# Logic.Training

Trainingsmodi des Spielers. Jede Trainingseinheit ist eine `TrainingSessionEntity` mit typabhängigen Detaildaten.

| Klasse | Modus |
|---|---|
| `SegmentTraining` | Ein Zielfeld über mehrere Aufnahmen treffen |
| `CheckoutTraining` | Checkouts von einem Startwert |
| `X01TrainingGame` | 301/501 allein spielen, ein Satz aus 5 Legs (Straight-/Double-In, Double-/Straight-Out); nach jedem Checkout startet das nächste Leg beim Startwert |
| `AroundTheClockTraining` | Segmente der Reihe nach |
| `Bobs27Training` | Bob's 27: D1 … D20 und Bull, Start mit 27 Punkten, 1–8 Spieler am selben Gerät, Eingabe = Treffer je Zug (0–3), Undo, Anfänger-Modus |

- `TrainingBase`: aktueller Benutzer, Laden eigener Trainings (`LoadSegmentTraining`, `LoadCheckoutTraining`, `LoadX01Training`, `LoadAroundTheClockTraining`), Wurfprüfung.
- **`TrainingId` ist immer die Id der `TrainingSession`** (Rückgabe von `Start…`). Geladen wird über `TrainingSessionId` und den Benutzer der Session.
- Jeder `AddThrow…` liefert ein Ergebnis je Dart (Rest, Bust/Checkout, Treffer, Ende), damit die Oberfläche keine Dart-Regeln nachbauen muss.
- Darts strikt der Reihe nach (Aufnahme/Versuch und Dart-Nummer werden geprüft).
- Checkout-Training: Versuch = bis zu 3 Darts auf den Zielwert, endet mit Checkout, Bust oder dem 3. Dart; immer Double-Out.
- Regeln aus `Logic.Shared/Rules`. Checkout-Wege für die Anzeige aus `CheckoutAdvisor` (`Shared.Models/Shared`): bester Weg zuerst, nach jedem Dart neu; nach Aufnahme-Ende, Bust oder einem beendeten Checkout-Versuch schon für die nächste Aufnahme (3 Darts).
- Around the Clock: Ziele 1–20, dann 25 (Bull zählt auch), dann Bull; Regeln nur in `Logic.Shared/Rules/AroundTheClockRules` (auch für die Statistik).
- Start: noch offene Trainings derselben Art werden beendet (`FinishOpenSessionsAsync`), es bleibt höchstens eins offen.
- Prüfungen beim Start: Checkout-Zielwert 2–170 und mit 3 Darts checkbar; Segment-Ziel kein Fehlwurf, 1–100 Aufnahmen.
- Fehler: ungültige Eingabe oder Reihenfolge → `ArgumentException` (400), Training schon beendet → `BusinessRuleException` (409).
- Bob's 27: Regeln und Ablauf in `Logic.Shared/Rules/Bobs27Rules` und `Bobs27Game` (Wertung, Ausscheiden bei ≤ 0, Reihenfolge, Sieger; Tests `Logic.Bot.Tests/Rules/Bobs27Tests`). Gespeichert werden nur die Züge (`Bobs27TrainingTurnEntity`), der Stand wird jedes Mal nachgespielt. Doppelt gesendete Züge (gleiche `TurnNumber`) → 409. Undo nimmt den letzten Zug zurück, auch direkt nach dem Spielende (nicht nach „Beenden“). Spieler 1 ist der Benutzer, Gäste nur mit Namen (beim Anonymisieren ersetzt).
- DI: `AddTrainingServices()` registriert alle fünf Services.

## Fortsetzen nach Reload

Jedes Training hat `GetRunningTrainingAsync()` (API: `GetRunning<Art>Training`, 204 wenn nichts offen ist).
Geliefert wird die neueste offene Session dieser Art des Benutzers mit allem, was die Seite zum Weiterspielen braucht:
Position des nächsten Darts, bereits geworfene Darts der angefangenen Aufnahme bzw. des Versuchs, Reststand/Ziel, Treffer, Busts, Checkout-Wege.
Die Werte werden aus den gespeicherten Würfen nach denselben Regeln wie in `AddThrowAsync` berechnet.
