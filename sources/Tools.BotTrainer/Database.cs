using Data.DartContext;
using Logic.Shared.DI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tools.BotTrainer
{
    /// <summary>DI mit DbContext und UnitOfWork wie in der API; Verbindung wird vor der eigentlichen Arbeit geprüft.</summary>
    public static class Database
    {
        public static async Task<ServiceProvider?> ConnectAsync(IConfiguration configuration)
        {
            var connectionString = ConnectionStrings.Resolve(configuration);
            Console.WriteLine($"Datenbank: {ConnectionStrings.Describe(connectionString)} (Quelle: {ConnectionStrings.Source(configuration)})");

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddDbContext<DartDatabaseContext>(o => o.UseMySQL(connectionString));
            services.AddSharedServices();
            var provider = services.BuildServiceProvider();

            await using var probe = provider.CreateAsyncScope();
            try
            {
                // OpenConnection statt CanConnect: CanConnect verschluckt den Grund.
                await probe.ServiceProvider.GetRequiredService<DartDatabaseContext>().Database.OpenConnectionAsync();
                return provider;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Keine Verbindung zur Datenbank: {exception.GetBaseException().Message}");
                await provider.DisposeAsync();
                return null;
            }
        }
    }
}
