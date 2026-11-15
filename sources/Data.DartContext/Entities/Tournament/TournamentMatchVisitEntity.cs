namespace Data.DartContext.Entities.Tournament
{
    /// <summary>Eine Aufnahme (bis zu 3 Darts) eines Spielers in einem Leg.</summary>
    public class TournamentMatchVisitEntity : AEntityBase
    {
        public int TournamentMatchLegId { get; set; }

        public TournamentMatchLegEntity Leg { get; set; } = null!;

        /// <summary>1 = Heim, 2 = Gast.</summary>
        public int PlayerNumber { get; set; }

        public int VisitNumber { get; set; }

        public int ScoreBefore { get; set; }

        public int ScoreAfter { get; set; }

        public bool IsBust { get; set; }

        public bool IsCheckout { get; set; }

        public int DartsUsed { get; set; }

        public ICollection<TournamentMatchThrowEntity> Throws { get; set; } = [];
    }
}
