using Shared.Enums.Statistics;

namespace Shared.Models.Statistics
{
    /// <summary>
    /// Statistik des angemeldeten Spielers für einen Zeitraum.
    /// Gezählt werden nur beendete Trainings mit mindestens einem Dart und beendete Bot-Spiele.
    /// </summary>
    public class PlayerStatistics
    {
        /// <summary>Tatsächlich ausgewerteter Zeitraum (kann bei leerem Zeitraum auf All ausweichen).</summary>
        public StatisticsPeriodEnum Period { get; set; }

        /// <summary>true, wenn der angefragte Zeitraum leer war und deshalb alles ausgewertet wurde.</summary>
        public bool IsFallbackToAll { get; set; }

        /// <summary>Beginn des Zeitraums (UTC), null bei All.</summary>
        public DateTime? From { get; set; }

        public bool HasData { get; set; }

        public SummaryStatistics Summary { get; set; } = new();

        public X01Statistics X01 { get; set; } = new();

        public CheckoutStatistics Checkout { get; set; } = new();

        public SegmentStatistics Segment { get; set; } = new();

        public AroundTheClockStatistics AroundTheClock { get; set; } = new();

        public BotGameStatistics BotGames { get; set; } = new();

        /// <summary>Beendete Turnier-Matches (offizielle und private).</summary>
        public TournamentMatchStatistics Tournaments { get; set; } = new();

        /// <summary>Punkte pro Aufnahme je X01-Session (bzw. Woche bei vielen Sessions), zeitlich aufsteigend.</summary>
        public List<StatisticsTrendPoint> X01AverageTrend { get; set; } = [];

        /// <summary>Elo nach jedem gewerteten Bot-Spiel (bzw. Woche), zeitlich aufsteigend.</summary>
        public List<StatisticsTrendPoint> RatingTrend { get; set; } = [];
    }

    public class SummaryStatistics
    {
        public int TrainingSessions { get; set; }

        public int X01Sessions { get; set; }

        public int CheckoutSessions { get; set; }

        public int SegmentSessions { get; set; }

        public int AroundTheClockSessions { get; set; }

        public int TotalDarts { get; set; }

        public int CurrentRating { get; set; }
    }

    public class X01Statistics
    {
        public int Sessions { get; set; }

        public int LegsFinished { get; set; }

        public int Darts { get; set; }

        public int Visits { get; set; }

        public decimal AveragePerDart { get; set; }

        /// <summary>„3-Dart-Average“: Punkte pro Aufnahme.</summary>
        public decimal AveragePerVisit { get; set; }

        public int BestVisit { get; set; }

        public int Busts { get; set; }

        /// <summary>Nur über ausgecheckte Legs; null ohne Checkout.</summary>
        public decimal? AverageDartsPerLeg { get; set; }
    }

    public class CheckoutStatistics
    {
        public int Sessions { get; set; }

        public int Attempts { get; set; }

        public int Successful { get; set; }

        public decimal Rate { get; set; }

        public decimal? AverageDartsPerCheckout { get; set; }

        /// <summary>Häufigste Zielwerte (max. 5).</summary>
        public List<CheckoutTargetStatistics> ByTarget { get; set; } = [];
    }

    public class CheckoutTargetStatistics
    {
        public int TargetScore { get; set; }

        public int Attempts { get; set; }

        public int Successful { get; set; }

        public decimal Rate { get; set; }
    }

    public class SegmentStatistics
    {
        public int Sessions { get; set; }

        public int Darts { get; set; }

        public int Hits { get; set; }

        public decimal HitRate { get; set; }

        /// <summary>Trefferquote je Zielfeld, häufigste zuerst.</summary>
        public List<SegmentTargetStatistics> ByTarget { get; set; } = [];
    }

    public class SegmentTargetStatistics
    {
        public int Segment { get; set; }

        public int Multiplier { get; set; }

        public int Darts { get; set; }

        public int Hits { get; set; }

        public decimal HitRate { get; set; }
    }

    public class AroundTheClockStatistics
    {
        public int Sessions { get; set; }

        /// <summary>Sessions, in denen alle Ziele bis Bull getroffen wurden.</summary>
        public int Completed { get; set; }

        public decimal HitRate { get; set; }

        public int? BestDarts { get; set; }

        public decimal? AverageDarts { get; set; }
    }

    public class BotGameStatistics
    {
        public int Games { get; set; }

        public int Wins { get; set; }

        public decimal WinRate { get; set; }

        /// <summary>Eigene Punkte pro Aufnahme in Bot-Spielen.</summary>
        public decimal AveragePerVisit { get; set; }

        /// <summary>Elo-Änderung im Zeitraum (letzter minus erster Wert), null ohne gewertete Spiele.</summary>
        public int? RatingChange { get; set; }
    }

    public class TournamentMatchStatistics
    {
        public int Matches { get; set; }

        public int Wins { get; set; }

        public decimal WinRate { get; set; }

        /// <summary>Eigene Punkte pro Aufnahme in Turnier-Matches.</summary>
        public decimal AveragePerVisit { get; set; }

        /// <summary>Elo-Änderung aus offiziellen Turnieren im Zeitraum, null ohne gewertete Matches.</summary>
        public int? RatingChange { get; set; }
    }

    public class StatisticsTrendPoint
    {
        public DateTime Date { get; set; }

        public decimal Value { get; set; }
    }
}
