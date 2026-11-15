using Shared.Enums.Statistics;
using Shared.Models.Statistics;

namespace Shared.Interfaces.Statistics
{
    public interface IPlayerStatisticsService
    {
        /// <summary>Statistik des angemeldeten Spielers; ein leerer 30/90-Tage-Zeitraum weicht auf All aus.</summary>
        Task<PlayerStatistics> GetStatisticsAsync(StatisticsPeriodEnum period);
    }
}
