using Data.DartContext.Entities.Bot;

namespace Shared.Interfaces.Bot
{
    /// <summary>
    /// Wertet ein abgeschlossenes Spiel für den Bot aus: XP, Elo, Level-Aufstieg mit neuer Profilversion.
    /// Arbeitet auf dem getrackten Spiel; gespeichert wird zusammen mit dem Spiel in einem SaveChangesAsync().
    /// </summary>
    public interface IDartBotLearningService
    {
        /// <summary>
        /// Gilt nur für abgeschlossene Spiele gegen Menschen und genau einmal je Bot-Teilnehmer
        /// (LearningProcessedAt). Andere Spiele werden ignoriert.
        /// </summary>
        Task ApplyGameResultAsync(DartBotGameEntity game, DateTime now);
    }
}
