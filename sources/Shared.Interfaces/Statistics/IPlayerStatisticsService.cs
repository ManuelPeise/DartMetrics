using Shared.Enums.Statistics;
using Shared.Models.Statistics;

namespace Shared.Interfaces.Statistics
{
    public interface IPlayerStatisticsService
    {
        /// <summary>
        /// Leistungs-Statistik des angemeldeten Spielers für eine Kategorie und einen Zeitraum [fromUtc, toUtc).
        /// fromUtc null = All Time (ohne Vorzeitraum-Vergleich), toUtc null = bis jetzt.
        /// </summary>
        Task<PlayerStatistics> GetStatisticsAsync(StatisticsCategoryEnum category, DateTime? fromUtc, DateTime? toUtc);
    }
}
