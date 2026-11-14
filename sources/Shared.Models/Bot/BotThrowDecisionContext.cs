namespace Shared.Models.Bot
{
    public sealed record BotThrowDecisionContext(
        int Remaining,
        int DartsAvailable,
        int OpponentRemaining,
        BotSkillProfile Profile);
}
