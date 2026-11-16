using System.Globalization;
using Logic.Statistics.Performance;
using Shared.Models.Progression;

namespace Logic.Statistics.Progression
{
    /// <summary>Persönliche Bestwerte aus dem ganzen Verlauf (werden nicht gespeichert, sondern immer neu berechnet).</summary>
    public static class RecordCalculator
    {
        /// <summary>Unter so vielen Darts aufs Doppel zählt die Checkout-Quote eines Spiels nicht als Rekord.</summary>
        public const int MinDoubleAttempts = 5;

        private static readonly CultureInfo German = new("de-DE");

        public static List<RecordItem> Calculate(ProgressionFacts facts)
        {
            var matchesByKey = facts.Matches.ToDictionary(m => m.UnitKey);
            var games = facts.GameLegs
                .GroupBy(l => l.UnitKey)
                .Where(g => matchesByKey.ContainsKey(g.Key))
                .Select(g => (Match: matchesByKey[g.Key], Legs: g.ToList()))
                .ToList();

            var records = new List<RecordItem>
            {
                Best("best-average", "Höchster Spiel-Ø", games
                    .Select(g => (Value: Average(g.Legs.SelectMany(l => l.Visits)), g.Match))
                    .Where(x => x.Value is not null)
                    .MaxOrNull(x => x.Value), x => Format(x.Value!.Value), x => x.Match),

                Best("best-first9", "Höchster First-9-Ø", games
                    .Select(g => (Value: Average(g.Legs.SelectMany(l => l.Visits.Take(3))), g.Match))
                    .Where(x => x.Value is not null)
                    .MaxOrNull(x => x.Value), x => Format(x.Value!.Value), x => x.Match),

                Best("most-180", "Meiste 180er in einem Spiel", games
                    .Select(g => (Value: g.Legs.Sum(l => l.Visits.Count(v => v.Points == 180)), g.Match))
                    .Where(x => x.Value > 0)
                    .MaxOrNull(x => x.Value), x => x.Value.ToString(German), x => x.Match),

                Best("best-checkout-rate", "Beste Checkout-Quote in einem Spiel", games
                    .Select(g => (Rate: CheckoutRate(g.Legs), g.Match))
                    .Where(x => x.Rate is not null)
                    .MaxOrNull(x => x.Rate), x => $"{x.Rate!.Value:0.0} %".Replace('.', ','), x => x.Match)
            };

            // Über alle Legs (auch X01-Training): höchstes Finish und kürzestes Leg ab 501.
            var finishes = facts.Legs
                .SelectMany(l => l.Visits.Where(v => v.IsCheckout).Select(v => (v.ScoreBefore, l)))
                .OrderByDescending(x => x.ScoreBefore).ThenBy(x => x.l.Date)
                .FirstOrDefault();
            records.Add(new RecordItem
            {
                Key = "highest-finish",
                Name = "Höchstes Finish",
                Value = finishes.l is null ? null : finishes.ScoreBefore.ToString(German),
                AchievedAt = finishes.l?.Date,
                Context = finishes.l is null ? null : ContextOf(finishes.l, matchesByKey)
            });

            var shortest = facts.Legs
                .Where(l => l.Visits.Count > 0 && l.Visits[0].ScoreBefore == 501 && l.Visits[^1].IsCheckout)
                .Select(l => (Darts: l.Visits.Sum(v => v.Darts.Count), Leg: l))
                .OrderBy(x => x.Darts).ThenBy(x => x.Leg.Date)
                .FirstOrDefault();
            records.Add(new RecordItem
            {
                Key = "fewest-darts",
                Name = "Kürzestes Leg (501)",
                Value = shortest.Leg is null ? null : $"{shortest.Darts} Darts",
                AchievedAt = shortest.Leg?.Date,
                Context = shortest.Leg is null ? null : ContextOf(shortest.Leg, matchesByKey)
            });

            records.Add(LongestWinStreak(facts));

            var aroundTheClock = facts.Trainings.Where(t => t.AroundTheClockDarts is not null).MinBy(t => t.AroundTheClockDarts);
            records.Add(new RecordItem
            {
                Key = "atc-best",
                Name = "Bestes Around the Clock",
                Value = aroundTheClock is null ? null : $"{aroundTheClock.AroundTheClockDarts} Darts",
                AchievedAt = aroundTheClock?.Date,
                Context = aroundTheClock is null ? null : "Training"
            });

            return records;
        }

        /// <summary>MaxBy ohne Ausnahme bei leerer Folge.</summary>
        private static T? MaxOrNull<T, TKey>(this IEnumerable<T> source, Func<T, TKey> key) where T : struct
        {
            var list = source.ToList();
            return list.Count == 0 ? null : list.MaxBy(key);
        }

        private static RecordItem Best<T>(string key, string name, T? best, Func<T, string> value, Func<T, MatchFact> match) where T : struct => new()
        {
            Key = key,
            Name = name,
            Value = best is { } b ? value(b) : null,
            AchievedAt = best is { } d ? match(d).Date : null,
            Context = best is { } c ? Describe(match(c)) : null
        };

        private static RecordItem LongestWinStreak(ProgressionFacts facts)
        {
            var current = 0;
            var best = 0;
            DateTime? bestAt = null;

            foreach (var match in facts.Matches.OrderBy(m => m.Date))
            {
                current = match.IsWon ? current + 1 : 0;

                if (current > best)
                {
                    best = current;
                    bestAt = match.Date;
                }
            }

            return new RecordItem
            {
                Key = "win-streak",
                Name = "Längste Siegesserie",
                Value = best == 0 ? null : $"{best} {(best == 1 ? "Spiel" : "Spiele")}",
                AchievedAt = bestAt
            };
        }

        private static double? Average(IEnumerable<StatVisit> visits)
        {
            var list = visits.ToList();
            var darts = list.Sum(v => v.Darts.Count);
            return darts == 0 ? null : Math.Round(list.Sum(v => v.Points) * 3.0 / darts, 2);
        }

        private static double? CheckoutRate(IReadOnlyCollection<StatLeg> legs)
        {
            var (attempts, successful) = StatisticsCalculator.DoubleAttempts(legs);
            return attempts < MinDoubleAttempts ? null : Math.Round(successful * 100.0 / attempts, 1);
        }

        private static string Format(double value) => value.ToString("0.00", German);

        private static string Describe(MatchFact match) =>
            match.IsTournament ? $"Turnier gegen {match.OpponentName}" : $"gegen {match.OpponentName}";

        private static string ContextOf(StatLeg leg, IReadOnlyDictionary<string, MatchFact> matches) =>
            matches.TryGetValue(leg.UnitKey, out var match) ? Describe(match) : "Training";
    }
}
