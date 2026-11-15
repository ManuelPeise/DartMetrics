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
    }
}
