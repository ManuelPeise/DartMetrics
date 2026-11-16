using global::Shared.Enums.Statistics;
using global::Shared.Models.Statistics;

namespace Web.App.Client.Services.Api
{
    public sealed class StatisticsApi : IStatisticsApi
    {
        private readonly ApiClient _api;

        public StatisticsApi(ApiClient api)
        {
            _api = api;
        }

        public Task<ApiResult<PlayerStatistics>> GetStatisticsAsync(StatisticsCategoryEnum category, DateTime? fromUtc, DateTime? toUtc,
            CancellationToken cancellationToken = default)
        {
            var query = $"category={(int)category}";

            if (fromUtc is { } from)
                query += "&from=" + Uri.EscapeDataString(from.ToUniversalTime().ToString("O"));

            if (toUtc is { } to)
                query += "&to=" + Uri.EscapeDataString(to.ToUniversalTime().ToString("O"));

            return ApiResponse.ReadAsync<PlayerStatistics>(_api.GetAsync($"PlayerStatistics/GetPlayerStatistics?{query}", cancellationToken), cancellationToken);
        }
    }
}
