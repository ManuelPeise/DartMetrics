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
            var dbContext = app.Services.CreateScope().ServiceProvider.GetRequiredService<DartDatabaseContext>();
            
            var pendingMigrations = dbContext.Database.GetPendingMigrations();

            if(pendingMigrations.Any())
            {
                dbContext.Database.Migrate();
            }

            var adminSeeder = new DefaultAdminSeed(_adminUserOptions);
            
            adminSeeder.Seed(dbContext);
        }
    }
}
