using Shared.Enums.Statistics;
using Shared.Enums.X01;

namespace Logic.Statistics.Performance
{
    /// <summary>Ein Dart des Spielers: getroffenes Feld und Reststand unmittelbar vor dem Dart.</summary>
    public sealed record StatDart(int Segment, int Multiplier, int RemainingBefore);

    /// <summary>Eine Aufnahme: gewertete Punkte (Bust = 0), Bust, Checkout, Reststand davor und die Darts.</summary>
    public sealed record StatVisit(int Points, bool IsBust, bool IsCheckout, int ScoreBefore, IReadOnlyList<StatDart> Darts);

    /// <summary>
    /// Ein X01-Leg des Spielers aus Training, Bot-Spiel oder Turnier, auf eine Form gebracht.
    /// UnitKey fasst Legs zu einem Match bzw. einer Session zusammen (Trends, Bestwerte).
    /// </summary>
    public sealed record StatLeg(
        string UnitKey,
        DateTime Date,
        StatisticsCategoryEnum Category,
        X01OutModeEnum OutMode,
        bool IsFinished,
        bool IsWon,
        IReadOnlyList<StatVisit> Visits);

    /// <summary>Ein abgeschlossenes Match (Bot-Spiel oder Turnier-Match). X01-Training allein ist kein Match.</summary>
    public sealed record StatMatch(string UnitKey, DateTime Date, StatisticsCategoryEnum Category, bool IsWon);
}
