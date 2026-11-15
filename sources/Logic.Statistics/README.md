# Logic.Statistics

Nur lesende Auswertung für den angemeldeten Spieler (`IPlayerStatisticsService`). Speichert nichts.

- **Zeitraum:** `StatisticsPeriodEnum` (30 Tage, 90 Tage, Gesamt), gezählt nach `FinishedAt` der Session bzw. des Spiels. Ist ein 30/90-Tage-Zeitraum leer, wird alles ausgewertet (`IsFallbackToAll`).
- **Zählt:** nur beendete Trainings mit mindestens einem Dart, nur beendete Bot-Spiele.
- **Inhalt:** Überblick (Sessions je Art, Darts, Elo), X01 (Average pro Dart/Aufnahme, beste Aufnahme, Busts, Darts pro Leg), Checkout (Quote gesamt und je Zielwert, Top 5), Segment (Trefferquote je Zielfeld, Top 5), Around the Clock (komplette Runden, Bestwert, Ø Darts), Bot-Spiele (Siege, Average, Elo-Änderung).
- **Verlauf:** X01-Average pro Session und Elo pro gewertetem Spiel; bei „Gesamt“ und mehr als 100 Punkten ein Wert pro Woche (Average = Mittel, Elo = letzter Wert).
- **Regeln:** X01-Punkte = `ScoreBefore − ScoreAfter` (Bust und Darts vor Double-In zählen 0), Around the Clock über `AroundTheClockRules`.
- **Datenzugriff:** UnitOfWork, `asNoTracking`, nur Vorwärts-Includes (keine Zyklen); Abfragen nacheinander (ein DbContext).
- **API:** `PlayerStatistics/GetPlayerStatistics?period=1|2|3`, nur Rolle Player.
- **DI:** `AddStatisticsServices()` in `Web.Core/Bundles/Services.cs`; im Dockerfile per `COPY Logic.Statistics/…` eingetragen.
