namespace Logic.Tournament.Knockout
{
    /// <summary>Ein Match im Baum. Runde 1 = erste Runde; <c>NextMatchNumber</c>/<c>NextSlot</c> = wohin der Sieger rückt (null im Finale).</summary>
    public sealed record BracketMatch(int Round, int MatchNumber, int? Home, int? Away, int? NextMatchNumber, int? NextSlot)
    {
        /// <summary>Freilos: genau eine Seite besetzt (nur in Runde 1).</summary>
        public bool IsBye => Round == 1 && (Home is null) != (Away is null);
    }

    /// <summary>
    /// K.-o.-Baum mit Standard-Setzung: Platz 1 und 2 treffen sich erst im Finale, Platz 1–4 erst im Halbfinale.
    /// Gesetzte bekommen die Setzplätze 1..k, Ungesetzte (in der übergebenen, bereits zufälligen Reihenfolge) die nächsten,
    /// die übrigen Plätze bis zur Zweierpotenz sind Freilose – sie treffen damit zuerst die Besten.
    /// Rein, ohne Datenbank.
    /// </summary>
    public static class KnockoutBracketBuilder
    {
        public const int MinParticipants = 4;
        public const int MaxParticipants = 64;

        public static int BracketSize(int participants)
        {
            var size = 2;
            while (size < participants)
            {
                size *= 2;
            }

            return size;
        }

        public static int RoundCount(int participants) => (int)Math.Log2(BracketSize(participants));

        /// <summary>Name einer Runde, gezählt vom Finale rückwärts.</summary>
        public static string RoundName(int round, int roundCount) => (roundCount - round) switch
        {
            0 => "Finale",
            1 => "Halbfinale",
            2 => "Viertelfinale",
            3 => "Achtelfinale",
            _ => $"Runde {round}"
        };

        /// <summary>Setzplätze in Baum-Reihenfolge, z. B. 8 → 1, 8, 4, 5, 2, 7, 3, 6 (jeweils zwei bilden ein Match).</summary>
        public static IReadOnlyList<int> SeedPositions(int size)
        {
            var positions = new List<int> { 1, 2 };

            while (positions.Count < size)
            {
                var next = positions.Count * 2 + 1;
                positions = positions.SelectMany(s => new[] { s, next - s }).ToList();
            }

            return positions;
        }

        public static IReadOnlyList<BracketMatch> Build(IReadOnlyList<int> seeded, IReadOnlyList<int> unseeded)
        {
            ArgumentNullException.ThrowIfNull(seeded);
            ArgumentNullException.ThrowIfNull(unseeded);

            var all = seeded.Concat(unseeded).ToList();

            if (all.Count is < MinParticipants or > MaxParticipants)
            {
                throw new ArgumentException($"A knockout needs {MinParticipants} to {MaxParticipants} participants.");
            }

            if (all.Distinct().Count() != all.Count)
            {
                throw new ArgumentException("Participants must be distinct.");
            }

            var size = BracketSize(all.Count);
            var rounds = RoundCount(all.Count);

            // Virtueller Setzplatz i (1-basiert) → Teilnehmer, über die Anzahl hinaus Freilos.
            int? BySeed(int seed) => seed <= all.Count ? all.ElementAt(seed - 1) : null;

            var positions = SeedPositions(size);
            var matches = new List<BracketMatch>();

            for (var round = 1; round <= rounds; round++)
            {
                var count = size >> round;

                for (var m = 1; m <= count; m++)
                {
                    int? home = null, away = null;

                    if (round == 1)
                    {
                        home = BySeed(positions.ElementAt(2 * (m - 1)));
                        away = BySeed(positions.ElementAt(2 * (m - 1) + 1));
                    }

                    var isFinal = round == rounds;
                    matches.Add(new BracketMatch(round, m, home, away,
                        isFinal ? null : (m + 1) / 2,
                        isFinal ? null : (m % 2 == 1 ? 1 : 2)));
                }
            }

            return matches;
        }
    }
}
