using Data.DartContext;
using Logic.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.Repositories;
using Shared.Models.Options;
using Logic.Shared.DI;
using Logic.Authentication.DI;
using Logic.Shared.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Service.Api.Controllers;
using Shared.Models.Authentication;
using Logic.Profile.DI;

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
            services.ConfigureJwtAuthentication(configuration);

            // Controllers live in the Service.Api project, not in Web.Core.
            services.AddControllers()
                .AddApplicationPart(typeof(ApiControllerBase).Assembly);
            services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

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

            if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
            {
                throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
            }

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Keep our own claim names (UserId, UserEmail, UserRole) instead of mapping them to SOAP URIs.
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),

                        NameClaimType = ClaimsHelper.UserEmailClaimType,
                        RoleClaimType = ClaimsHelper.UserRolesClaimType
                    };
                });

            services.AddAuthorization();
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

            services.AddOptions<EmailOptions>()
                .Bind(configuration.GetSection("Email"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<ServerOptions>()
                .Bind(configuration.GetSection("Server"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        private static void AddDiServices(this IServiceCollection services)
        {
            services.AddSharedServices();
            services.AddAuthenticationServices();
            services.AddProfileServices();
        }
    }
}
