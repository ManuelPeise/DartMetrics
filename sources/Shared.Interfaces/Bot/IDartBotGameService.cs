using Shared.Models.Bot;

namespace Shared.Interfaces.Bot
{
    /// <summary>Spiele Mensch gegen Bot (301/501, Double-Out, Best of X Legs).</summary>
    public interface IDartBotGameService
    {
        /// <summary>Startet ein Spiel. Hat der Bot den Anwurf, spielt er seine erste Aufnahme sofort.</summary>
        Task<DartBotGameState> StartGameAsync(StartDartBotGameModel model);

        /// <summary>Erfasst die Aufnahme des Menschen; danach spielt der Bot, bis der Mensch wieder am Zug ist.</summary>
        Task<DartBotGameState> AddUserVisitAsync(DartBotUserVisitModel model);

        /// <summary>Bricht ein laufendes Spiel ab (ohne Lernen und ohne XP).</summary>
        Task AbortGameAsync(int gameId);

        Task<DartBotGameState> GetGameAsync(int gameId);

        /// <summary>Laufendes Spiel des aktuellen Benutzers (das zuletzt gestartete) oder null.</summary>
        Task<DartBotGameState?> GetRunningGameAsync();

        /// <summary>Aktive Bots für die Gegnerwahl, sortiert nach Level.</summary>
        Task<List<DartBotSummary>> GetAvailableBotsAsync();
    }
}
