namespace Logic.Shared.Rules
{
    /// <summary>
    /// Bob's 27: Start mit 27 Punkten, 21 Runden auf D1 … D20 und Bull, je Runde 3 Darts aufs Ziel.
    /// Treffer: + Treffer × Zielwert; kein Treffer: − Zielwert. Bei 0 oder weniger Punkten scheidet man aus
    /// (außer im Anfänger-Modus). Höchstwert: 27 + 3 × (2 + 4 + … + 40 + 50) = 1437.
    /// </summary>
    public static class Bobs27Rules
    {
        public const int StartScore = 27;
        public const int DartsPerRound = 3;
        public const int RoundCount = 21;
        public const int BullValue = 50;
        public const int MaxScore = 1437;

        /// <summary>Zielwert der Runde 1–21: D1 = 2 … D20 = 40, Runde 21 = Bull (50).</summary>
        public static int TargetValue(int round)
        {
            if (round is < 1 or > RoundCount)
            {
                throw new ArgumentOutOfRangeException(nameof(round), round, "Round must be between 1 and 21.");
            }

            return round == RoundCount ? BullValue : round * 2;
        }

        /// <summary>Kurzform für die Anzeige: D1 … D20, Bull.</summary>
        public static string TargetLabel(int round) => round == RoundCount ? "Bull" : $"D{round}";

        /// <summary>Feld des Ziels als (Segment, Multiplier): D1 … D20 bzw. Bull = 25 × 2.</summary>
        public static (int Segment, int Multiplier) TargetField(int round) => round == RoundCount ? (25, 2) : (round, 2);

        public static bool IsValidHits(int hits) => hits is >= 0 and <= DartsPerRound;

        /// <summary>Neuer Stand nach einer Runde: mit Treffern + Treffer × Zielwert, ohne Treffer − Zielwert.</summary>
        public static int ApplyRound(int score, int round, int hits)
        {
            if (!IsValidHits(hits))
            {
                throw new ArgumentOutOfRangeException(nameof(hits), hits, "Hits must be between 0 and 3.");
            }

            var value = TargetValue(round);
            return hits > 0 ? score + hits * value : score - value;
        }

        /// <summary>Ausgeschieden bei 0 oder weniger Punkten (0 ist schon Game Over), nicht im Anfänger-Modus.</summary>
        public static bool IsBust(int score, bool beginnerMode) => !beginnerMode && score <= 0;
    }
}
