using Shared.Models.Administration.Dashboard;

namespace Shared.Interfaces.Administration
{
    public interface IDashboardStatisticsService
    {
        Task<DashboardStatistics> GetStatisticsAsync();
    }
}
