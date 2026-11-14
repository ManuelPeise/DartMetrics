using global::Shared.Models.Bot;

namespace Web.App.Client.Services.Api
{
    /// <summary>Spiele gegen DartBots (DartBotGameController).</summary>
    public interface IBotGameApi
    {
        Task<ApiResult<List<DartBotSummary>>> GetAvailableBotsAsync(CancellationToken cancellationToken = default);

        /// <summary>Laufendes Spiel oder null (zum Fortsetzen nach dem Neuladen).</summary>
        Task<ApiResult<DartBotGameState?>> GetRunningGameAsync(CancellationToken cancellationToken = default);

        Task<ApiResult<List<DartBotRatingHistoryEntry>>> GetRatingHistoryAsync(int take = 20, CancellationToken cancellationToken = default);

        Task<ApiResult<DartBotGameState>> StartGameAsync(StartDartBotGameModel model, CancellationToken cancellationToken = default);

        /// <summary>Aufnahme des Menschen; die Antwort enthält bereits die Aufnahme(n) des Bots.</summary>
        Task<ApiResult<DartBotGameState>> AddVisitAsync(DartBotUserVisitModel model, CancellationToken cancellationToken = default);

        Task<ApiResult<bool>> AbortGameAsync(int gameId, CancellationToken cancellationToken = default);

        Task<ApiResult<DartBotGameState>> GetGameAsync(int gameId, CancellationToken cancellationToken = default);
    }
}
