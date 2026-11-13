using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Administration;

namespace Logic.Administration.DI
{
    public static class AdministrationServiceRegistration
    {
        public static void AddAdministrationServices(this IServiceCollection services)
        {
            services.AddScoped<ILogService, LogService>();
        }
    }
}
