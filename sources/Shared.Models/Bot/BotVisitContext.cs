using Shared.Enums.X01;

namespace Shared.Models.Bot
{
    /// <summary>Spielsituation zu Beginn einer Bot-Aufnahme.</summary>
    /// <param name="IsOpened">Bei Double-In: wurde das Leg bereits eröffnet?</param>
    public sealed record BotVisitContext(
        int ScoreBefore,
        int OpponentScoreBefore,
        BotSkillProfile Profile,
        X01InModeEnum InMode = X01InModeEnum.Straight,
        X01OutModeEnum OutMode = X01OutModeEnum.Double,
        bool IsOpened = true);
}
