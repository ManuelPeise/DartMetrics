namespace Data.DartContext.Entities.Bot
{
    /// <summary>Ein Leg innerhalb eines Spiels. Der Startwert steht am Spiel.</summary>
    public class DartBotGameLegEntity : AEntityBase
    {
        public int DartBotGameId { get; set; }

        public DartBotGameEntity Game { get; set; } = null!;

        public int LegNumber { get; set; }

        public int StartingPlayerId { get; set; }

        public DartBotGamePlayerEntity StartingPlayer { get; set; } = null!;

        public int? WinnerPlayerId { get; set; }

        public DartBotGamePlayerEntity? WinnerPlayer { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public ICollection<DartBotGameVisitEntity> Visits { get; set; } = [];
    }
}
