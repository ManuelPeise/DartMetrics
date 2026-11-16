using Shared.Enums.Statistics;

namespace Shared.Models.Statistics
{
    /// <summary>
    /// Leistungs-Statistik des angemeldeten Spielers für eine Kategorie und einen Zeitraum.
    /// Alle Averages werden aus Punkten und Darts berechnet (nie als Mittel von Match-Averages).
    /// Werte ohne ausreichende Daten sind null (Anzeige "–").
    /// </summary>
    public class PlayerStatistics
    {
        public StatisticsCategoryEnum Category { get; set; }

        /// <summary>Beginn des Zeitraums (UTC, inklusiv); null = All Time.</summary>
        public DateTime? From { get; set; }

        /// <summary>Ende des Zeitraums (UTC, exklusiv); null = bis jetzt.</summary>
        public DateTime? To { get; set; }

        /// <summary>Gleich langer Zeitraum direkt davor (für die Vergleiche); null bei All Time.</summary>
        public DateTime? PreviousFrom { get; set; }

        public DateTime? PreviousTo { get; set; }

        public bool HasData { get; set; }

        /// <summary>Alle geworfenen Darts im Zeitraum (Legs und Übungen).</summary>
        public int TotalDarts { get; set; }

        public int CurrentRating { get; set; }

        public PerformanceKpis Kpis { get; set; } = new();

        public ScoringStatistics Scoring { get; set; } = new();

        public MatchCheckoutStatistics Checkout { get; set; } = new();

        public LegMatchStatistics LegsAndMatches { get; set; } = new();

        public DevelopmentStatistics Development { get; set; } = new();

        /// <summary>Automatische Hinweise (nur bei ausreichend Daten).</summary>
        public List<string> Insights { get; set; } = [];

        /// <summary>3-Dart- und First-9-Average je Match/Session, Woche oder Monat.</summary>
        public List<AverageTrendPoint> AverageTrend { get; set; } = [];

        /// <summary>Checkout-Quote je Woche oder Monat.</summary>
        public List<StatisticsTrendPoint> CheckoutTrend { get; set; } = [];

        public List<StatisticsTrendPoint> RatingTrend { get; set; } = [];

        /// <summary>Übungen ohne Legs (nur Kategorie Training und Overall).</summary>
        public ExerciseStatistics Exercises { get; set; } = new();
    }

    /// <summary>Aktueller Wert und Wert im Vorzeitraum (null = nicht berechenbar).</summary>
    public class ComparedValue
    {
        public decimal? Current { get; set; }

        public decimal? Previous { get; set; }

        /// <summary>Current - Previous; null, wenn einer fehlt.</summary>
        public decimal? Change => Current is { } c && Previous is { } p ? c - p : null;
    }

    public class PerformanceKpis
    {
        public ComparedValue ThreeDartAverage { get; set; } = new();

        public ComparedValue FirstNineAverage { get; set; } = new();

        /// <summary>Prozent: erfolgreiche Doppel-Würfe / Darts auf ein Doppel (nur Double-Out).</summary>
        public ComparedValue CheckoutRate { get; set; } = new();

        /// <summary>Prozent: gewonnene / gespielte Matches (Bot-Spiele, Turnier-Matches).</summary>
        public ComparedValue WinRate { get; set; } = new();
    }

    public class ScoringStatistics
    {
        public int? HighestScore { get; set; }

        public int Visits { get; set; }

        public int Count180 { get; set; }

        /// <summary>Kumuliert: alle Aufnahmen mit mindestens 140 Punkten (inklusive 180).</summary>
        public int Count140Plus { get; set; }

        public int Count100Plus { get; set; }

        public int Count80Plus { get; set; }

        public int Count60Plus { get; set; }

        public decimal? Per100Legs180 { get; set; }

        /// <summary>Verteilung der Aufnahmen in getrennten Bändern (0–19 … 180).</summary>
        public List<ScoreBand> Distribution { get; set; } = [];
    }

    public class ScoreBand
    {
        public string Label { get; set; } = "";

        public int Min { get; set; }

        public int Max { get; set; }

        public int Count { get; set; }
    }

    /// <summary>Checkouts in Legs. Quote und Versuche nur aus Double-Out-Legs; Highest/Ø Checkout und Busts aus allen Legs.</summary>
    public class MatchCheckoutStatistics
    {
        public decimal? Rate { get; set; }

        /// <summary>Doppel-Treffer, die das Leg beendet haben (Double-Out).</summary>
        public int Successful { get; set; }

        /// <summary>Darts, die bei einem Ein-Dart-Finish geworfen wurden (Rest gerade ≤ 40 oder 50).</summary>
        public int Attempts { get; set; }

        public int MissedDoubles { get; set; }

        public int? HighestCheckout { get; set; }

        public decimal? AverageCheckout { get; set; }

        public int Busts { get; set; }
    }

    public class LegMatchStatistics
    {
        public int MatchesPlayed { get; set; }

        public int MatchesWon { get; set; }

        public decimal? WinRate { get; set; }

        /// <summary>Abgeschlossene Legs (unterbrochene Legs zählen nicht).</summary>
        public int LegsPlayed { get; set; }

        public int LegsWon { get; set; }

        public decimal? LegsWonRate { get; set; }

        /// <summary>Darts pro gewonnenem Leg.</summary>
        public decimal? AverageDartsPerLeg { get; set; }

        public int? BestLegDarts { get; set; }

        public int LongestWinningStreak { get; set; }
    }

    public class DevelopmentStatistics
    {
        /// <summary>Letzte 10 Matches gegen die 10 davor (Current/Previous); null-Werte bei weniger als 20 Matches.</summary>
        public DevelopmentComparison LastTenMatches { get; set; } = new();

        /// <summary>Persönliche Bestwerte in der Kategorie (über alle Zeit, je Match bzw. Session).</summary>
        public PersonalBests PersonalBests { get; set; } = new();
    }

    public class DevelopmentComparison
    {
        public ComparedValue ThreeDartAverage { get; set; } = new();

        public ComparedValue FirstNineAverage { get; set; } = new();

        public ComparedValue CheckoutRate { get; set; } = new();

        public ComparedValue WinRate { get; set; } = new();
    }

    public class PersonalBests
    {
        /// <summary>Bester 3-Dart-Average eines Matches bzw. einer Session (mindestens 30 Darts).</summary>
        public decimal? ThreeDartAverage { get; set; }

        public decimal? FirstNineAverage { get; set; }

        /// <summary>Beste Checkout-Quote eines Matches bzw. einer Session (mindestens 5 Versuche).</summary>
        public decimal? CheckoutRate { get; set; }
    }

    public class AverageTrendPoint
    {
        public DateTime Date { get; set; }

        public decimal ThreeDartAverage { get; set; }

        public decimal? FirstNineAverage { get; set; }

        public int Legs { get; set; }
    }

    public class ExerciseStatistics
    {
        public CheckoutStatistics Checkout { get; set; } = new();

        public SegmentStatistics Segment { get; set; } = new();

        public AroundTheClockStatistics AroundTheClock { get; set; } = new();

        public bool HasData => Checkout.Sessions > 0 || Segment.Sessions > 0 || AroundTheClock.Sessions > 0;
    }

    public class CheckoutStatistics
    {
        public int Sessions { get; set; }

        public int Darts { get; set; }

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

        public int Darts { get; set; }

        public decimal HitRate { get; set; }

        public int? BestDarts { get; set; }

        public decimal? AverageDarts { get; set; }
    }

    public class StatisticsTrendPoint
    {
        public DateTime Date { get; set; }

        public decimal Value { get; set; }
    }
}
