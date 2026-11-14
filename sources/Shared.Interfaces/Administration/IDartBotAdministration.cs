using Shared.Models.Bot;

namespace Shared.Interfaces.Administration
{
    /// <summary>Verwaltung der Bots (nur Administratoren).</summary>
    public interface IDartBotAdministration
    {
        /// <summary>Legt einen Bot auf Level 0 mit Profilversion 1 an und gibt seine Id zurück.</summary>
        Task<int> CreateBotAsync(CreateDartBotModel model);

        Task DeactivateBotAsync(int botId);

        Task ActivateBotAsync(int botId);

        Task<List<DartBotListItem>> GetBotsAsync(bool includeInactive = false);

        Task<DartBotListItem> GetBotAsync(int botId);
    }
}
