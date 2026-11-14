namespace Shared.Models.Bot
{
    public sealed record BotVisitContext(
       int ScoreBefore,
       int OpponentScoreBefore,
       BotSkillProfile Profile);
}
