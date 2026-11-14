using Shared.Enums.Bot;
using Shared.Enums.X01;

namespace Shared.Models.Bot
{
    /// <summary>Spielstand aus Sicht des Menschen (Spieler 1 = Mensch, Spieler 2 = Bot).</summary>
    public class DartBotGameState
    {
        public int GameId { get; set; }

        public DartBotGameModeEnum GameMode { get; set; }

        public DartBotGameStatusEnum Status { get; set; }

        public int StartingScore { get; set; }

        public X01InModeEnum InMode { get; set; }

        public X01OutModeEnum OutMode { get; set; }

        /// <summary>Bei Double-In: hat der Mensch bzw. der Bot das aktuelle Leg schon eröffnet?</summary>
        public bool IsUserOpened { get; set; } = true;

        public bool IsBotOpened { get; set; } = true;

        public int BestOfLegs { get; set; }

        public int BotId { get; set; }

        public string BotName { get; set; } = null!;

        public int UserLegsWon { get; set; }

        public int BotLegsWon { get; set; }

        public int CurrentLegNumber { get; set; }

        public int UserRemaining { get; set; }

        public int BotRemaining { get; set; }

        /// <summary>Ist der Mensch am Zug? Bei beendeten Spielen false.</summary>
        public bool IsUserTurn { get; set; }

        /// <summary>null solange das Spiel läuft oder abgebrochen wurde.</summary>
        public bool? IsUserWinner { get; set; }

        /// <summary>Nach Spielende: XP und Elo-Änderung des Bots (sonst null).</summary>
        public long? BotXpGained { get; set; }

        public int? BotRatingBefore { get; set; }

        public int? BotRatingAfter { get; set; }

        /// <summary>Nach Spielende: Elo-Änderung des Spielers (sonst null).</summary>
        public int? UserRatingBefore { get; set; }

        public int? UserRatingAfter { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        /// <summary>Alle Aufnahmen des aktuellen (bzw. letzten) Legs in zeitlicher Reihenfolge.</summary>
        public List<DartBotVisitSummary> CurrentLegVisits { get; set; } = [];

        /// <summary>
        /// Aufnahmen des vorherigen Legs (leer im ersten Leg). Nach einem Leg-Wechsel enthält es den entscheidenden
        /// Checkout, der in CurrentLegVisits nicht mehr vorkommt.
        /// </summary>
        public List<DartBotVisitSummary> PreviousLegVisits { get; set; } = [];
    }

    public class DartBotVisitSummary
    {
        public int LegNumber { get; set; }

        public int TurnNumber { get; set; }

        public bool IsBot { get; set; }

        public int ScoreBefore { get; set; }

        public int ScoreAfter { get; set; }

        public bool IsBust { get; set; }

        public bool IsCheckout { get; set; }

        public int DartsUsed { get; set; }

        public List<DartBotThrowSummary> Throws { get; set; } = [];
    }

    public class DartBotThrowSummary
    {
        public int ThrowNumber { get; set; }

        public int Segment { get; set; }

        public int Multiplier { get; set; }

        public int Score { get; set; }

        public int? TargetSegment { get; set; }

        public int? TargetMultiplier { get; set; }
    }
}
