using Data.DartContext;
using Data.DartContext.Seeds;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Options;

namespace Web.Core.Bundles
{
    internal class DatabaseInitialization
    {
        private readonly AdminUserOptions _adminUserOptions;

        public DatabaseInitialization(AdminUserOptions adminUserOptions)
        {
            _adminUserOptions = adminUserOptions;
        }

        public void Initialize(WebApplication app)
        {
            // Dispose the scope (and with it the DbContext/connection) after initialization.
            using var scope = app.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<DartDatabaseContext>();

            // Migrate() itself only applies what is missing - no need to check pending migrations first.
            dbContext.Database.Migrate();

            var adminSeeder = new DefaultAdminSeed(_adminUserOptions);
            
            adminSeeder.Seed(dbContext);
        }
    }
}
