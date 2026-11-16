using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    /// <summary>Steckbrief eines Bots: Stärke, Eigenschaften, Kennzahlen aus echten Spielen, Formverlauf und letzte Spiele.</summary>
    public class DartBotProfileCard
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? StageName { get; set; }
        public float? BaseAverage { get; set; }
        public float? PotentialAverage { get; set; }
        public int Rating { get; set; }
        public DartBotPlayingStyleEnum? PlayingStyle { get; set; }

        // Eigenschaften (0–1)
        public float Form { get; set; }
        public float Pressure { get; set; }
        public float Nerves { get; set; }
        public float RouteKnowledge { get; set; }
        public float Risk { get; set; }

        // Kennzahlen aus beendeten Spielen
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public double? Average { get; set; }
        public double? First9Average { get; set; }

        /// <summary>Treffer je Dart auf ein Finish-Doppel (0–1).</summary>
        public double? CheckoutRate { get; set; }
        public int? HighestFinish { get; set; }
        public int OneEighties { get; set; }

        /// <summary>Tendenz der letzten Spiele: 1 steigend, 0 stabil, −1 fallend.</summary>
        public int FormTrend { get; set; }

        /// <summary>Letzte Spiele, älteste zuerst (für Verlauf und S/N-Reihe).</summary>
        public List<DartBotProfileCardGame> RecentGames { get; set; } = [];
    }

    public class DartBotProfileCardGame
    {
        public int GameId { get; set; }
        public DateTime PlayedAt { get; set; }
        public string OpponentName { get; set; } = "";
        public bool IsWin { get; set; }
        public int LegsWon { get; set; }
        public int LegsLost { get; set; }
        public float? Average { get; set; }

        /// <summary>Tagesform (Faktor auf die Streuung, 1 = normal, kleiner = besser).</summary>
        public float? DayForm { get; set; }
    }
}
