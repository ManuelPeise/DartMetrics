using Shared.Enums.X01;
using Shared.Models.Statistics;

namespace Logic.Statistics.Performance
{
    /// <summary>
    /// Reine Berechnung aller Leistungs-Kennzahlen aus vereinheitlichten Legs und Matches (ohne Datenbank, direkt testbar).
    /// Regeln:
    /// - Averages immer aus Summe Punkte / Summe Darts × 3, nie als Mittel von Match-Averages.
    /// - First 9: die ersten drei Aufnahmen (höchstens 9 Darts) jedes Legs.
    /// - Doppel-Versuch: Dart bei einem Ein-Dart-Finish (Rest gerade 2–40 oder 50), nur in Double-Out-Legs.
    ///   Treffer = der Dart, mit dem das Leg gecheckt wurde. Quote = Treffer / Versuche.
    /// - Leg-Kennzahlen nur aus abgeschlossenen Legs; Average und Scoring zählen auch Aufnahmen unterbrochener Legs.
    /// - Keine Daten → null statt 0, damit nichts Falsches angezeigt wird.
    /// </summary>
    public static class StatisticsCalculator
    {
        public const int FirstNineVisits = 3;

        /// <summary>Mindestmenge für Bestwerte eines Matches bzw. einer Session.</summary>
        public const int MinDartsForBestAverage = 30;

        public const int MinAttemptsForBestCheckoutRate = 5;

        public const int RecentMatchCount = 10;

        private static readonly (string Label, int Min, int Max)[] Bands =
        [
            ("0–19", 0, 19), ("20–39", 20, 39), ("40–59", 40, 59), ("60–79", 60, 79), ("80–99", 80, 99),
            ("100–119", 100, 119), ("120–139", 120, 139), ("140–159", 140, 159), ("160–179", 160, 179), ("180", 180, 180)
        ];

        // ---------- Grundwerte ----------

        public static decimal? ThreeDartAverage(IEnumerable<StatLeg> legs)
        {
            var visits = legs.SelectMany(l => l.Visits).ToList();
            return Average(visits.Sum(v => v.Points), visits.Sum(v => v.Darts.Count));
        }

        public static decimal? FirstNineAverage(IEnumerable<StatLeg> legs)
        {
            var visits = legs.SelectMany(l => l.Visits.Take(FirstNineVisits)).ToList();
            return Average(visits.Sum(v => v.Points), visits.Sum(v => v.Darts.Count));
        }

        public static (int Attempts, int Successful) DoubleAttempts(IEnumerable<StatLeg> legs)
        {
            var attempts = 0;
            var successful = 0;

            foreach (var leg in legs.Where(l => l.OutMode == X01OutModeEnum.Double))
            {
                foreach (var visit in leg.Visits)
                {
                    for (var i = 0; i < visit.Darts.Count; i++)
                    {
                        if (!IsOneDartFinish(visit.Darts[i].RemainingBefore))
                        {
                            continue;
                        }

                        attempts++;

                        if (visit.IsCheckout && i == visit.Darts.Count - 1)
                        {
                            successful++;
                        }
                    }
                }
            }

            return (attempts, successful);
        }

        public static decimal? CheckoutRate(IEnumerable<StatLeg> legs)
        {
            var (attempts, successful) = DoubleAttempts(legs);
            return Percent(successful, attempts);
        }

        public static decimal? WinRate(IEnumerable<StatMatch> matches)
        {
            var list = matches.ToList();
            return Percent(list.Count(m => m.IsWon), list.Count);
        }

        /// <summary>Ein Dart kann checken: Double-Out mit geradem Rest 2–40 oder Bull (50).</summary>
        public static bool IsOneDartFinish(int remaining) => remaining == 50 || remaining is >= 2 and <= 40 && remaining % 2 == 0;

        // ---------- Bereiche ----------

        public static PerformanceKpis Kpis(
            IReadOnlyList<StatLeg> legs, IReadOnlyList<StatMatch> matches,
            IReadOnlyList<StatLeg>? previousLegs, IReadOnlyList<StatMatch>? previousMatches) => new()
        {
            ThreeDartAverage = Compare(ThreeDartAverage(legs), previousLegs is null ? null : ThreeDartAverage(previousLegs)),
            FirstNineAverage = Compare(FirstNineAverage(legs), previousLegs is null ? null : FirstNineAverage(previousLegs)),
            CheckoutRate = Compare(CheckoutRate(legs), previousLegs is null ? null : CheckoutRate(previousLegs)),
            WinRate = Compare(WinRate(matches), previousMatches is null ? null : WinRate(previousMatches))
        };

        public static ScoringStatistics Scoring(IReadOnlyList<StatLeg> legs)
        {
            var points = legs.SelectMany(l => l.Visits).Select(v => v.Points).ToList();
            var finishedLegs = legs.Count(l => l.IsFinished);
            var count180 = points.Count(p => p == 180);

            return new ScoringStatistics
            {
                HighestScore = points.Count == 0 ? null : points.Max(),
                Visits = points.Count,
                Count180 = count180,
                Count140Plus = points.Count(p => p >= 140),
                Count100Plus = points.Count(p => p >= 100),
                Count80Plus = points.Count(p => p >= 80),
                Count60Plus = points.Count(p => p >= 60),
                Per100Legs180 = finishedLegs == 0 ? null : Round(count180 * 100m / finishedLegs),
                Distribution = Bands
                    .Select(b => new ScoreBand { Label = b.Label, Min = b.Min, Max = b.Max, Count = points.Count(p => p >= b.Min && p <= b.Max) })
                    .ToList()
            };
        }

        public static MatchCheckoutStatistics Checkout(IReadOnlyList<StatLeg> legs)
        {
            var (attempts, successful) = DoubleAttempts(legs);
            var checkouts = legs.SelectMany(l => l.Visits).Where(v => v.IsCheckout).Select(v => v.ScoreBefore).ToList();

            return new MatchCheckoutStatistics
            {
                Rate = Percent(successful, attempts),
                Successful = successful,
                Attempts = attempts,
                MissedDoubles = attempts - successful,
                HighestCheckout = checkouts.Count == 0 ? null : checkouts.Max(),
                AverageCheckout = checkouts.Count == 0 ? null : Round((decimal)checkouts.Average()),
                Busts = legs.SelectMany(l => l.Visits).Count(v => v.IsBust)
            };
        }

        public static LegMatchStatistics LegsAndMatches(IReadOnlyList<StatLeg> legs, IReadOnlyList<StatMatch> matches)
        {
            var finished = legs.Where(l => l.IsFinished).ToList();
            var won = finished.Where(l => l.IsWon).ToList();
            var wonDarts = won.Select(l => l.Visits.Sum(v => v.Darts.Count)).ToList();
            var matchesWon = matches.Count(m => m.IsWon);

            return new LegMatchStatistics
            {
                MatchesPlayed = matches.Count,
                MatchesWon = matchesWon,
                WinRate = Percent(matchesWon, matches.Count),
                LegsPlayed = finished.Count,
                LegsWon = won.Count,
                LegsWonRate = Percent(won.Count, finished.Count),
                AverageDartsPerLeg = wonDarts.Count == 0 ? null : Round((decimal)wonDarts.Average()),
                BestLegDarts = wonDarts.Count == 0 ? null : wonDarts.Min(),
                LongestWinningStreak = LongestStreak(matches)
            };
        }

        /// <summary>Letzte 10 Matches gegen die 10 davor; Werte nur, wenn es mindestens 20 Matches gibt.</summary>
        public static DevelopmentComparison LastMatches(IReadOnlyList<StatLeg> legs, IReadOnlyList<StatMatch> matches)
        {
            var ordered = matches.OrderByDescending(m => m.Date).ToList();

            if (ordered.Count < 2 * RecentMatchCount)
            {
                return new DevelopmentComparison();
            }

            var recent = ordered.Take(RecentMatchCount).ToList();
            var before = ordered.Skip(RecentMatchCount).Take(RecentMatchCount).ToList();
            var recentLegs = LegsOf(legs, recent);
            var beforeLegs = LegsOf(legs, before);

            return new DevelopmentComparison
            {
                ThreeDartAverage = Compare(ThreeDartAverage(recentLegs), ThreeDartAverage(beforeLegs)),
                FirstNineAverage = Compare(FirstNineAverage(recentLegs), FirstNineAverage(beforeLegs)),
                CheckoutRate = Compare(CheckoutRate(recentLegs), CheckoutRate(beforeLegs)),
                WinRate = Compare(WinRate(recent), WinRate(before))
            };
        }

        /// <summary>Bestwerte je Match bzw. Session (mit Mindestmenge, damit kein Zufallswert gewinnt).</summary>
        public static PersonalBests PersonalBests(IReadOnlyList<StatLeg> legs)
        {
            var units = legs.GroupBy(l => l.UnitKey).Select(g => g.ToList()).ToList();

            var averages = units
                .Where(u => u.Sum(l => l.Visits.Sum(v => v.Darts.Count)) >= MinDartsForBestAverage)
                .Select(u => (ThreeDart: ThreeDartAverage(u), FirstNine: FirstNineAverage(u)))
                .ToList();

            var checkoutRates = units
                .Select(DoubleAttempts)
                .Where(a => a.Attempts >= MinAttemptsForBestCheckoutRate)
                .Select(a => Percent(a.Successful, a.Attempts))
                .ToList();

            return new PersonalBests
            {
                ThreeDartAverage = averages.Max(a => a.ThreeDart),
                FirstNineAverage = averages.Max(a => a.FirstNine),
                CheckoutRate = checkoutRates.Max()
            };
        }

        /// <summary>Ein Punkt je Match/Session; ab 30 Einheiten je Woche, ab 26 Wochen je Monat.</summary>
        public static List<AverageTrendPoint> AverageTrend(IReadOnlyList<StatLeg> legs)
        {
            var groups = Group(legs);

            return groups
                .Select(g => (g.Date, Legs: g.Legs, Average: ThreeDartAverage(g.Legs)))
                .Where(g => g.Average is not null)
                .Select(g => new AverageTrendPoint
                {
                    Date = g.Date,
                    ThreeDartAverage = g.Average!.Value,
                    FirstNineAverage = FirstNineAverage(g.Legs),
                    Legs = g.Legs.Count(l => l.IsFinished)
                })
                .ToList();
        }

        /// <summary>Checkout-Quote je Woche (bzw. Monat bei langen Zeiträumen), nur Gruppen mit Versuchen.</summary>
        public static List<StatisticsTrendPoint> CheckoutTrend(IReadOnlyList<StatLeg> legs)
        {
            var byMonth = SpanDays(legs) > 26 * 7;

            return legs
                .GroupBy(l => byMonth ? MonthStart(l.Date) : WeekStart(l.Date))
                .OrderBy(g => g.Key)
                .Select(g => (Date: g.Key, Rate: CheckoutRate(g)))
                .Where(g => g.Rate is not null)
                .Select(g => new StatisticsTrendPoint { Date = g.Date, Value = g.Rate!.Value })
                .ToList();
        }

        // ---------- Hilfen ----------

        public static ComparedValue Compare(decimal? current, decimal? previous) => new() { Current = current, Previous = previous };

        private static List<StatLeg> LegsOf(IReadOnlyList<StatLeg> legs, IReadOnlyList<StatMatch> matches)
        {
            var keys = matches.Select(m => m.UnitKey).ToHashSet();
            return legs.Where(l => keys.Contains(l.UnitKey)).ToList();
        }

        private static int LongestStreak(IReadOnlyList<StatMatch> matches)
        {
            var longest = 0;
            var current = 0;

            foreach (var match in matches.OrderBy(m => m.Date))
            {
                current = match.IsWon ? current + 1 : 0;
                longest = Math.Max(longest, current);
            }

            return longest;
        }

        private static List<(DateTime Date, List<StatLeg> Legs)> Group(IReadOnlyList<StatLeg> legs)
        {
            var units = legs.GroupBy(l => l.UnitKey).Select(g => (Date: g.Max(l => l.Date), Legs: g.ToList())).OrderBy(u => u.Date).ToList();

            if (units.Count <= 30)
            {
                return units;
            }

            var byMonth = SpanDays(legs) > 26 * 7;

            return legs
                .GroupBy(l => byMonth ? MonthStart(l.Date) : WeekStart(l.Date))
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, g.ToList()))
                .ToList();
        }

        private static double SpanDays(IReadOnlyList<StatLeg> legs) =>
            legs.Count < 2 ? 0 : (legs.Max(l => l.Date) - legs.Min(l => l.Date)).TotalDays;

        private static DateTime WeekStart(DateTime date)
        {
            var offset = ((int)date.DayOfWeek + 6) % 7;
            return DateTime.SpecifyKind(date.Date.AddDays(-offset), DateTimeKind.Utc);
        }

        private static DateTime MonthStart(DateTime date) => new(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        private static decimal? Average(int points, int darts) => darts == 0 ? null : Round(points * 3m / darts);

        public static decimal? Percent(int value, int count) => count == 0 ? null : Round(value * 100m / count);

        private static decimal Round(decimal value) => Math.Round(value, 2);
    }
}
