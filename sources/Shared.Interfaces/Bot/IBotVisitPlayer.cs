using Shared.Models.Bot;

namespace Shared.Interfaces.Bot
{
    public interface IBotVisitPlayer
    {
        BotVisitResult PlayVisit(BotVisitContext context);

        /// <summary>Wie oben, aber mit einer bestimmten Zielwahl (z. B. Hybrid mit Modell).</summary>
        BotVisitResult PlayVisit(BotVisitContext context, ITargetSelector targetSelector);
    }
}
