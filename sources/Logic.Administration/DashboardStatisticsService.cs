using Logic.Shared;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Administration.Dashboard;

namespace Logic.Administration
{
    /// <summary>Zählwerte für das Admin-Dashboard. Zugriff nur für Admins (Controller).</summary>
    public class DashboardStatisticsService : ALogicBase, IDashboardStatisticsService
    {
        private const int RecentDays = 7;

        public DashboardStatisticsService(IUnitOfWork unitOfWork, IMessageLog messageLog)
            : base(unitOfWork, messageLog)
        {
        }

        public async Task<DashboardStatistics> GetStatisticsAsync()
        {
            try
            {
                var since = DateTime.UtcNow.AddDays(-RecentDays);
                var trainings = UnitOfWork.DartTrainingUnitOfWork.TrainingSessionRepository;

                // Nacheinander: ein DbContext erlaubt keine parallelen Abfragen.
                return new DashboardStatistics
                {
                    TrainingSessionCount = await trainings.CountAsync(),
                    TrainingSessionsLast7Days = await trainings.CountAsync(t => t.StartedAt >= since)
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading dashboard statistics.", nameof(DashboardStatisticsService), exception);
                throw;
            }
        }
    }
}
