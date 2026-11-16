namespace Logic.Bot.Ml
{
    /// <summary>Parameter der Kandidaten-Bewertung (Plan-Doc „ML.NET Phase 1“).</summary>
    public sealed class RolloutOptions
    {
        /// <summary>Simulierte Legs je Kandidat (N).</summary>
        public int RolloutsPerCandidate { get; init; } = 50;

        /// <summary>Obergrenze je Rollout; wird sie erreicht, zählt der Rollout als so viele Darts.</summary>
        public int MaxDartsPerLeg { get; init; } = 60;

        /// <summary>Grundwert für die Zufallsströme; gleicher Seed = gleiche Labels.</summary>
        public int Seed { get; init; } = 1;

        /// <summary>
        /// Rollouts nach Rest verteilen: im Finish-Bereich (≤ 100) unterscheiden sich Kandidaten nur knapp, dort doppelt so
        /// viele; ab 301 halb so viele. Im Mittel über die Situationen etwa gleich viel Rechenzeit.
        /// </summary>
        public bool AdaptiveRollouts { get; init; }

        public int RolloutsFor(int remaining)
        {
            if (!AdaptiveRollouts)
            {
                return RolloutsPerCandidate;
            }

            return remaining <= 100 ? RolloutsPerCandidate * 2
                : remaining > 300 ? Math.Max(1, RolloutsPerCandidate / 2)
                : RolloutsPerCandidate;
        }
    }
}
