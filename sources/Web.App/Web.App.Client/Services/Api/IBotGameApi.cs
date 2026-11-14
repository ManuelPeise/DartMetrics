using global::Shared.Models.Bot;

namespace Web.App.Client.Services.Api
{
    /// <summary>Spiele gegen DartBots (DartBotGameController). Weitere Methoden folgen mit der Bot-Spielseite.</summary>
    public interface IBotGameApi
    {
        Task<ApiResult<List<DartBotSummary>>> GetAvailableBotsAsync(CancellationToken cancellationToken = default);
    }
}
