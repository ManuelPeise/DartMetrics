namespace Data.DartContext.Entities.Tournament
{
    /// <summary>Ein Dart. Bei Menschen gibt ihn der GameMaster ein, bei Bots kommt er aus der Simulation (mit Ziel).</summary>
    public class TournamentMatchThrowEntity : AEntityBase
    {
        public int TournamentMatchVisitId { get; set; }

        public TournamentMatchVisitEntity Visit { get; set; } = null!;

        public int ThrowNumber { get; set; }

        public int? TargetSegment { get; set; }

        public int? TargetMultiplier { get; set; }

        public int Segment { get; set; }

        public int Multiplier { get; set; }

        public int Score { get; set; }

        public int ScoreBeforeThrow { get; set; }
    }
}
