namespace Data.DartContext.Entities.Tournament
{
    public class TournamentMatchLegEntity : AEntityBase
    {
        public int TournamentMatchId { get; set; }

        public TournamentMatchEntity Match { get; set; } = null!;

        public int LegNumber { get; set; }

        /// <summary>1 = Heim, 2 = Gast; wechselt von Leg zu Leg.</summary>
        public int StartingPlayerNumber { get; set; }

        public int? WinnerPlayerNumber { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public ICollection<TournamentMatchVisitEntity> Visits { get; set; } = [];
    }
}
