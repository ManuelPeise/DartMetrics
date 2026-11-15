using Shared.Enums.X01;

namespace Shared.Models.Bot.Ml
{
    /// <summary>
    /// Spielsituation vor einem Dart, für Kandidaten-Bewertung und ML-Merkmale.
    /// ScoreAtVisitStart: Stand zu Beginn der laufenden Aufnahme (Bust fällt dorthin zurück);
    /// bei DartsAvailable = 3 gleich Remaining.
    /// </summary>
    public sealed record BotSituation(
        int Remaining,
        int DartsAvailable,
        int OpponentRemaining,
        BotSkillProfile Profile,
        X01InModeEnum InMode = X01InModeEnum.Straight,
        X01OutModeEnum OutMode = X01OutModeEnum.Double,
        bool IsOpened = true,
        int? ScoreAtVisitStart = null)
    {
        public int VisitStart => ScoreAtVisitStart ?? Remaining;

        /// <summary>Bei Straight-In ist das Leg immer eröffnet.</summary>
        public bool Opened => IsOpened || InMode == X01InModeEnum.Straight;
    }
}
