using Shared.Models.Bot;

namespace Shared.Interfaces.Bot
{
    public interface ITargetSelector
    {
        DartTarget SelectTarget(BotThrowDecisionContext context);
    }
}
