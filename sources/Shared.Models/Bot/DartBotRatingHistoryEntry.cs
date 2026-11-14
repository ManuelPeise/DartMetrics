namespace Shared.Models.Bot
{
    /// <summary>Elo-Änderung des Spielers aus einem abgeschlossenen Bot-Spiel.</summary>
    public class DartBotRatingHistoryEntry
    {
        public int GameId { get; set; }

        public DateTime FinishedAt { get; set; }

        public string OpponentName { get; set; } = null!;

        public bool IsWin { get; set; }

        public int RatingBefore { get; set; }

        public int RatingAfter { get; set; }
    }
}
