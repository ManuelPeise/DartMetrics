namespace Shared.Models.Bot
{
    /// <summary>Ergebnis einer Bot-Aufnahme.</summary>
    public class BotVisitResult
    {
        public int ScoreBefore { get; set; }

        public int ScoreAfter { get; set; }

        public bool IsBust { get; set; }

        public bool IsCheckout { get; set; }

        public int DartsUsed => Throws.Count;

        public List<BotThrowResult> Throws { get; set; } = [];
    }
}
