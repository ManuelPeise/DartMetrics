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

        public Task<ApiResult<PlayerStatistics>> GetStatisticsAsync(StatisticsPeriodEnum period, CancellationToken cancellationToken = default) =>
            ApiResponse.ReadAsync<PlayerStatistics>(
                _api.GetAsync($"PlayerStatistics/GetPlayerStatistics?period={(int)period}", cancellationToken), cancellationToken);
    }
}
