using Logic.Statistics.Performance;

namespace Logic.Statistics.Progression
{
    /// <summary>Ein beendetes Spiel (Bot-Spiel oder Turnier-Match) des Spielers.</summary>
    public sealed record MatchFact(
        string UnitKey,
        DateTime Date,
        bool IsTournament,
        bool IsWon,
        int LegsWon,
        string OpponentName,
        float? BotOpponentAverage,
        int? TournamentId,
        bool WonTournament);

    /// <summary>Eine beendete Trainings-Session; bei Around the Clock mit der Anzahl Darts.</summary>
    public sealed record TrainingFact(DateTime Date, bool IsAroundTheClock, int? AroundTheClockDarts);

    /// <summary>Alles, was Level, Erfolge und Rekorde brauchen, chronologisch sortiert.</summary>
    public sealed class ProgressionFacts
    {
        public required IReadOnlyList<StatLeg> Legs { get; init; }
        public required IReadOnlyList<MatchFact> Matches { get; init; }
        public required IReadOnlyList<TrainingFact> Trainings { get; init; }
        public required DateTime RegisteredAt { get; init; }
        public required DateTime Now { get; init; }

        /// <summary>Legs aus Spielen (nicht X01-Training allein).</summary>
        public IEnumerable<StatLeg> GameLegs => Legs.Where(l => !l.UnitKey.StartsWith("x01-", StringComparison.Ordinal));
    }
}
