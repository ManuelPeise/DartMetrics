# Logic.Statistics

Nur lesende Leistungs-Statistik des angemeldeten Spielers (`IPlayerStatisticsService`). Speichert nichts.

## Ablauf

1. **Laden** (`PlayerStatisticsService`): alle eigenen X01-Legs aus X01-Training, Bot-Spielen und Turnier-Matches über alle Zeit, vereinheitlicht als `Performance/StatLeg` (Aufnahmen mit Punkten, Bust, Checkout und Reststand je Dart) und `StatMatch`.
   - X01-Training: Reststand je Dart aus dem Rest vor der Aufnahme abgeleitet (Double-In: Darts vor dem Eröffnen zählen 0, `X01Rules.IsOpeningDart`).
   - Bot-Spiele und Turniere: Reststand je Dart ist gespeichert (`ScoreBeforeThrow`).
   - Nur beendete Sessions/Matches; abgebrochene Bot-Spiele zählen nicht. Datum = Ende der Session bzw. des Matches.
2. **Filtern** nach Kategorie und Zeitraum `[from, to)`; Vorzeitraum = gleich lang direkt davor (nicht bei All Time).
3. **Rechnen** mit `Performance/StatisticsCalculator` (rein, ohne Datenbank, Unit-Tests in `Logic.Statistics.Tests`) und `Performance/PerformanceInsights`.

## Kategorien

| Kategorie | Inhalt |
|---|---|
| Training | X01-Training allein, alle Bot-Spiele (Modus Training und Match); dazu die Übungen (Checkout-, Segment-, Around-the-Clock-Training) als eigener Abschnitt |
| Official | Turnier-Matches (Liga, K.-o.; auch gegen Bots, auch private Turniere) |
| Overall | beides, ohne Doppelzählung |

## Definitionen

- **3-Dart-Average:** Summe Punkte / Summe Darts × 3 (Bust = 0 Punkte, Darts zählen). Nie als Mittel von Match-Averages.
- **First-9-Average:** die ersten drei Aufnahmen jedes Legs, gleiche Formel.
- **Doppel-Versuch:** Dart bei einem Ein-Dart-Finish (Rest gerade 2–40 oder 50), nur in Double-Out-Legs. **Checkout-Quote** = Darts, die das Leg beendet haben / Doppel-Versuche; Missed Doubles = Versuche − Treffer.
- **Highest/Ø Checkout, Busts:** aus allen Legs (auch Straight-Out).
- **Legs:** nur abgeschlossene Legs; Ø Darts pro Leg und bestes Leg aus gewonnenen Legs. Aufnahmen unterbrochener Legs zählen trotzdem für Average und Scoring.
- **Matches:** Bot-Spiele und Turnier-Matches (X01-Training allein ist kein Match); Siegquote, längste Siegesserie.
- **Score-Bereiche:** Kacheln kumuliert (140+ enthält 180), Verteilung in getrennten Bändern.
- **Entwicklung:** Zeitraum gegen Vorzeitraum, letzte 10 gegen die 10 Matches davor (ab 20 Matches), persönliche Bestwerte je Match/Session (ab 30 Darts bzw. 5 Doppel-Versuchen).
- **Hinweise:** nur ab 10 abgeschlossenen Legs in beiden Zeiträumen und Änderungen ab 2 Punkten (Average) bzw. 3 Prozentpunkten (Quote); feste Texte.
- **Verlauf:** ein Punkt je Match/Session, ab 30 Einheiten je Woche, ab 26 Wochen Spanne je Monat.
- Ohne Daten ist ein Wert `null` (Anzeige „–“), nie 0.

## Technik

- **Datenzugriff:** UnitOfWork, `asNoTracking`, nur Vorwärts-Includes; Abfragen nacheinander (ein DbContext).
- **API:** `PlayerStatistics/GetPlayerStatistics?category=1|2|3&from=…&to=…` (UTC, ISO 8601), nur Rolle Player. Die Seite rechnet „Dieser Monat“, „Dieses Jahr“ und eigene Zeiträume in Ortszeit aus.
- **DI:** `AddStatisticsServices()` in `Web.Core/Bundles/Services.cs`; im Dockerfile per `COPY Logic.Statistics/…` eingetragen.
