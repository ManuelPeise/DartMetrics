using Data.DartContext;
using Logic.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.Repositories;
using Shared.Models.Options;
using Logic.Shared.DI;
using Logic.Authentication.DI;
using Service.Api.Controllers;
using Shared.Models.Authentication;

namespace Web.Core.Bundles
{
    internal static class Services
    {
        internal static void AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            ApplyOptions(services, configuration);
            services.AddHttpContextAccessor();
            services.AddDatabaseServices(configuration);
            services.AddRepositories();

            // Controllers live in the Service.Api project, not in Web.Core.
            services.AddControllers()
                .AddApplicationPart(typeof(ApiControllerBase).Assembly);
            services.AddOpenApi();

            // Add shared services from projects that are referenced by this project
            services.AddDiServices();


        }

        private static void AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DartContext");

            ArgumentException.ThrowIfNullOrEmpty(connectionString);

            services.AddDbContext<DartDatabaseContext>(options =>
                options.UseMySQL(connectionString));
        }

        private static void ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>();
            if (jwtOptions is null)
            {
                throw new InvalidOperationException("JWT options are not configured.");
            }
           
        }

        private static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
        }

        private static void ApplyOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<AdminUserOptions>()
                .Bind(configuration.GetSection("AdminUser"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection("Jwt"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        private static void AddDiServices(this IServiceCollection services)
        {
            services.AddSharedServices();
            services.AddAuthenticationServices();
        }
    }
}
