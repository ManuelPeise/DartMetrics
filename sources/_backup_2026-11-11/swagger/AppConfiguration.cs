using Shared.Models.Options;

namespace Web.Core.Bundles
{
    public static class AppConfiguration
    {
        public static void ConfigureApp(this WebApplication app, IConfiguration configuration)
        {
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            InitializeDatabase(app, configuration);
        }

        private static void InitializeDatabase(WebApplication app, IConfiguration configuration)
        {
            var adminUserOptions = configuration.GetSection("AdminUser").Get<AdminUserOptions>();

            if (adminUserOptions is null)
            {
                throw new InvalidOperationException("Admin user options are not configured.");
            }

            var database = new DatabaseInitialization(adminUserOptions);

            database.Initialize(app);

        }
        
    }
}
