using Shared.Enums.X01;

namespace Shared.Models.Bot
{
    /// <summary>Situation vor einem einzelnen Dart.</summary>
    /// <param name="Remaining">Reststand vor diesem Dart.</param>
    /// <param name="DartsAvailable">Darts in dieser Aufnahme inklusive des aktuellen (3, 2 oder 1).</param>
    /// <param name="IsOpened">Bei Double-In: ist das Leg bereits eröffnet?</param>
    /// <param name="ScoreAtVisitStart">Stand zu Beginn der Aufnahme (bei Bust fällt der Stand darauf zurück); null = Remaining.</param>
    public sealed record BotThrowDecisionContext(
        int Remaining,
        int DartsAvailable,
        int OpponentRemaining,
        BotSkillProfile Profile,
        X01InModeEnum InMode = X01InModeEnum.Straight,
        X01OutModeEnum OutMode = X01OutModeEnum.Double,
        bool IsOpened = true,
        int? ScoreAtVisitStart = null);
}
