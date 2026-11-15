using Shared.Enums.Tournament;
using Shared.Enums.X01;
using Shared.Models.Shared;

namespace Shared.Models.Tournament
{
    /// <summary>Ein Dart, den der Spielleiter für den Menschen am Wurf eingibt.</summary>
    public class AddTournamentDartModel
    {
        /// <summary>0 = Miss, 1–20, 25 = Bull.</summary>
        public int Segment { get; set; }

        /// <summary>0 bei Miss, 1–3, Bull 1–2.</summary>
        public int Multiplier { get; set; }

        /// <summary>Stand, auf dem die Eingabe beruht; weicht er ab (zweites Gerät), antwortet der Server mit 409.</summary>
        public Guid? ConcurrencyStamp { get; set; }
    }

    /// <summary>Vollständiger Spielstand für den Eingabe-Screen und Live-Ansichten.</summary>
    public class TournamentMatchState
    {
        public int MatchId { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = "";
        public TournamentTypeEnum TournamentType { get; set; }
        public string RoundName { get; set; } = "";
        public int BestOfLegs { get; set; }
        public int LegsToWin { get; set; }
        public int StartingScore { get; set; }
        public X01InModeEnum InMode { get; set; }
        public X01OutModeEnum OutMode { get; set; }
        public TournamentMatchStatusEnum Status { get; set; }
        public DateTime? ScheduledAt { get; set; }

        public TournamentMatchSide Home { get; set; } = new();
        public TournamentMatchSide Away { get; set; } = new();

        /// <summary>1 = Heim, 2 = Gast; nach Matchende der Sieger.</summary>
        public int CurrentPlayer { get; set; }
        public int LegNumber { get; set; }
        public int LegStarter { get; set; }

        /// <summary>Darts der laufenden Aufnahme (0–2).</summary>
        public List<TournamentDartItem> CurrentVisit { get; set; } = [];

        /// <summary>Darf der angemeldete Benutzer gerade eingeben (Spielleiter und ein Mensch am Wurf)?</summary>
        public bool CanEnterDarts { get; set; }

        /// <summary>Gibt es einen Dart eines Menschen, den der Spielleiter zurücknehmen kann?</summary>
        public bool CanUndo { get; set; }

        /// <summary>Finish-Wege für den Spieler am Wurf (leer, wenn kein Finish möglich).</summary>
        public List<CheckoutSuggestion> CheckoutSuggestions { get; set; } = [];

        public int? WinnerParticipantId { get; set; }
        public Guid ConcurrencyStamp { get; set; }
        public List<TournamentLegItem> Legs { get; set; } = [];
    }

    public class TournamentMatchSide
    {
        public int? ParticipantId { get; set; }
        public string DisplayName { get; set; } = "";
        public TournamentParticipantTypeEnum? ParticipantType { get; set; }
        public int LegsWon { get; set; }
        public int Remaining { get; set; }
        public bool IsOpened { get; set; }

        /// <summary>3-Dart-Average im Match (Bust und Darts vor Double-In zählen 0).</summary>
        public double Average { get; set; }

        /// <summary>Elo vor/nach dem Match (nur offizielle Turniere, nach Matchende).</summary>
        public int? RatingBefore { get; set; }
        public int? RatingAfter { get; set; }

        /// <summary>Nur Bots: XP aus diesem Match.</summary>
        public long? XpGained { get; set; }
    }

    public class TournamentLegItem
    {
        public int LegNumber { get; set; }
        public int StartingPlayer { get; set; }
        public int? WinnerPlayer { get; set; }
        public List<TournamentVisitItem> Visits { get; set; } = [];
    }

    public class TournamentVisitItem
    {
        public int PlayerNumber { get; set; }
        public int VisitNumber { get; set; }
        public int ScoreBefore { get; set; }
        public int ScoreAfter { get; set; }
        public bool IsBust { get; set; }
        public bool IsCheckout { get; set; }
        public List<TournamentDartItem> Darts { get; set; } = [];
    }

    public class TournamentDartItem
    {
        public int Segment { get; set; }
        public int Multiplier { get; set; }
        public int Score { get; set; }
    }
}
