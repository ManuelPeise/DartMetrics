using Shared.Models.Bot;

namespace Shared.Interfaces.Bot
{
    public interface IBotVisitPlayer
    {
        BotVisitResult PlayVisit(BotVisitContext context);
    }
}
