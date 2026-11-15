using global::Shared.Enums.Statistics;
using global::Shared.Models.Statistics;

namespace Web.App.Client.Services.Api
{
    /// <summary>Eigene Statistik (PlayerStatisticsController).</summary>
    public interface IStatisticsApi
    {
        Task<ApiResult<PlayerStatistics>> GetStatisticsAsync(StatisticsPeriodEnum period, CancellationToken cancellationToken = default);
    }
}
