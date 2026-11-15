using Shared.Models.Options;
using Web.Core.MiddleWare;

namespace Web.Core.Bundles
{
    public static class AppConfiguration
    {
        public static void ConfigureApp(this WebApplication app, IConfiguration configuration)
        {
            if (app.Environment.IsDevelopment())
            {
                // OpenAPI document: /openapi/v1.json, Swagger UI: /swagger
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "DartMetrics API v1");
                    options.EnablePersistAuthorization();
                });
            }

            app.UseHttpsRedirection();
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            app.UseAuthentication();
            app.UseMiddleware<MustChangePasswordMiddleware>();
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
