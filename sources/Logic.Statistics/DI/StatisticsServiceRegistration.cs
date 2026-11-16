using Microsoft.Extensions.DependencyInjection;
using Logic.Statistics.Progression;
using Shared.Interfaces.Progression;
using Shared.Interfaces.Statistics;

namespace Logic.Statistics.DI
{
    public static class StatisticsServiceRegistration
    {
        public static void AddStatisticsServices(this IServiceCollection services)
        {
            services.AddScoped<IPlayerStatisticsService, PlayerStatisticsService>();
            services.AddScoped<IProgressionService, ProgressionService>();
        }
    }
}
