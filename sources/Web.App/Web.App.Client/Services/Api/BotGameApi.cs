using global::Shared.Models.Bot;

namespace Web.App.Client.Services.Api
{
    public sealed class BotGameApi : IBotGameApi
    {
        private const string Controller = "DartBotGame/";

        private readonly ApiClient _api;

        public BotGameApi(ApiClient api)
        {
            _api = api;
        }

        public Task<ApiResult<List<DartBotSummary>>> GetAvailableBotsAsync(CancellationToken ct = default) =>
            ApiResponse.ReadAsync<List<DartBotSummary>>(_api.GetAsync(Controller + "GetAvailableDartBots", ct), ct);

        public Task<ApiResult<DartBotProfileCard>> GetBotProfileCardAsync(int botId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<DartBotProfileCard>(_api.GetAsync($"{Controller}GetDartBotProfileCard?botId={botId}", ct), ct);

        public Task<ApiResult<List<DartBotRatingHistoryEntry>>> GetRatingHistoryAsync(int take = 20, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<List<DartBotRatingHistoryEntry>>(_api.GetAsync($"{Controller}GetDartBotRatingHistory?take={take}", ct), ct);

        public Task<ApiResult<DartBotGameState?>> GetRunningGameAsync(CancellationToken ct = default) =>
            ApiResponse.ReadOptionalAsync<DartBotGameState>(_api.GetAsync(Controller + "GetRunningDartBotGame", ct), ct);

        public Task<ApiResult<DartBotGameState>> StartGameAsync(StartDartBotGameModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<DartBotGameState>(_api.PostAsJsonAsync(Controller + "StartDartBotGame", model, ct), ct);

        public Task<ApiResult<DartBotGameState>> AddVisitAsync(DartBotUserVisitModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<DartBotGameState>(_api.PostAsJsonAsync(Controller + "AddDartBotGameVisit", model, ct), ct);

        public Task<ApiResult<bool>> AbortGameAsync(int gameId, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync<object?>($"{Controller}AbortDartBotGame?gameId={gameId}", null, ct), ct);

        public Task<ApiResult<DartBotGameState>> GetGameAsync(int gameId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<DartBotGameState>(_api.GetAsync($"{Controller}GetDartBotGame?gameId={gameId}", ct), ct);
    }
}
