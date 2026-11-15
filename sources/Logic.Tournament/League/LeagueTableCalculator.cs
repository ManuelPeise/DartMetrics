namespace Logic.Tournament.League
{
    /// <summary>Ergebnis eines beendeten Liga-Matches mit den Punkten (für den Average) je Seite.</summary>
    public sealed record LeagueMatchResult(int Home, int Away, int HomeLegs, int AwayLegs, int HomeScored, int HomeDarts, int AwayScored, int AwayDarts);

    public sealed class LeagueTableEntry
    {
        public int ParticipantId { get; init; }
        public string DisplayName { get; init; } = "";
        public int Position { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public int Points { get; set; }
        public int LegsFor { get; set; }
        public int LegsAgainst { get; set; }
        public int LegDifference => LegsFor - LegsAgainst;
        public int Scored { get; set; }
        public int Darts { get; set; }

        /// <summary>3-Dart-Average über alle Liga-Matches.</summary>
        public double Average => Darts == 0 ? 0 : Math.Round(Scored * 3.0 / Darts, 2);
    }

    /// <summary>
    /// Liga-Tabelle: 2 Punkte pro Sieg. Reihenfolge bei Gleichstand: Punkte, Leg-Differenz, gewonnene Legs,
    /// direkter Vergleich (Punkte, dann Leg-Differenz unter den Gleichstehenden), Average, zuletzt der Name (stabil).
    /// </summary>
    public static class LeagueTableCalculator
    {
        public const int PointsPerWin = 2;

        public static List<LeagueTableEntry> Calculate(IReadOnlyDictionary<int, string> participants, IEnumerable<LeagueMatchResult> results)
        {
            var list = results.ToList();
            var entries = participants.ToDictionary(p => p.Key, p => new LeagueTableEntry { ParticipantId = p.Key, DisplayName = p.Value });

            foreach (var r in list)
            {
                if (!entries.TryGetValue(r.Home, out var home) || !entries.TryGetValue(r.Away, out var away))
                {
                    throw new ArgumentException("Result references an unknown participant.");
                }

                if (r.HomeLegs == r.AwayLegs)
                {
                    throw new ArgumentException("A league match has no draw.");
                }

                Add(home, r.HomeLegs, r.AwayLegs, r.HomeScored, r.HomeDarts);
                Add(away, r.AwayLegs, r.HomeLegs, r.AwayScored, r.AwayDarts);
            }

            var ordered = entries.Values
                .GroupBy(e => (e.Points, e.LegDifference, e.LegsFor))
                .OrderByDescending(g => g.Key.Points)
                .ThenByDescending(g => g.Key.LegDifference)
                .ThenByDescending(g => g.Key.LegsFor)
                .SelectMany(g => g.Count() == 1 ? g : HeadToHead(g.ToList(), list))
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].Position = i + 1;
            }

            return ordered;
        }

        private static void Add(LeagueTableEntry entry, int legsFor, int legsAgainst, int scored, int darts)
        {
            entry.Played++;
            entry.LegsFor += legsFor;
            entry.LegsAgainst += legsAgainst;
            entry.Scored += scored;
            entry.Darts += darts;

            if (legsFor > legsAgainst)
            {
                entry.Won++;
                entry.Points += PointsPerWin;
            }
            else
            {
                entry.Lost++;
            }
        }

        /// <summary>Mini-Tabelle nur aus den Matches der Gleichstehenden untereinander.</summary>
        private static IEnumerable<LeagueTableEntry> HeadToHead(List<LeagueTableEntry> tied, List<LeagueMatchResult> results)
        {
            var ids = tied.Select(e => e.ParticipantId).ToHashSet();
            var points = ids.ToDictionary(id => id, _ => 0);
            var difference = ids.ToDictionary(id => id, _ => 0);

            foreach (var r in results.Where(r => ids.Contains(r.Home) && ids.Contains(r.Away)))
            {
                var winner = r.HomeLegs > r.AwayLegs ? r.Home : r.Away;
                points[winner] += PointsPerWin;
                difference[r.Home] += r.HomeLegs - r.AwayLegs;
                difference[r.Away] += r.AwayLegs - r.HomeLegs;
            }

            return tied
                .OrderByDescending(e => points.GetValueOrDefault(e.ParticipantId))
                .ThenByDescending(e => difference.GetValueOrDefault(e.ParticipantId))
                .ThenByDescending(e => e.Average)
                .ThenBy(e => e.DisplayName, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
