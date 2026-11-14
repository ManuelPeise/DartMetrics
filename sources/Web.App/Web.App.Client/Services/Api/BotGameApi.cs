using global::Shared.Models.Bot;

namespace Web.App.Client.Services.Api
{
    public sealed class BotGameApi : IBotGameApi
    {
        private readonly ApiClient _api;

        public BotGameApi(ApiClient api)
        {
            _api = api;
        }

        public Task<ApiResult<List<DartBotSummary>>> GetAvailableBotsAsync(CancellationToken cancellationToken = default) =>
            ApiResponse.ReadAsync<List<DartBotSummary>>(_api.GetAsync("DartBotGame/GetAvailableDartBots", cancellationToken), cancellationToken);
    }
}
