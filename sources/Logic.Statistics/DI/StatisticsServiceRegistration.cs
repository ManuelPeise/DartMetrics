using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Statistics;

namespace Logic.Statistics.DI
{
    public static class StatisticsServiceRegistration
    {
        public static void AddStatisticsServices(this IServiceCollection services)
        {
            services.AddScoped<IPlayerStatisticsService, PlayerStatisticsService>();
        }
    }
}
