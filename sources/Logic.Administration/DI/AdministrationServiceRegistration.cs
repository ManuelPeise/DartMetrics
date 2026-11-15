using Microsoft.Extensions.DependencyInjection;
using Logic.Administration.DataRetention;
using Shared.Interfaces.Administration;

namespace Logic.Administration.DI
{
    public static class AdministrationServiceRegistration
    {
        public static void AddAdministrationServices(this IServiceCollection services)
        {
            services.AddScoped<ILogService, LogService>();
            services.AddScoped<IUserAdministration, UserAdministration>();
            services.AddScoped<IDashboardStatisticsService, DashboardStatisticsService>();

            // Löschfristen (unbestätigte Konten, Log-Einträge), siehe DataRetentionOptions.
            services.AddHostedService<DataRetentionWorker>();
        }
    }
}
