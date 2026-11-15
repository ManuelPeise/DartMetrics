using Data.DartContext;
using Data.DartContext.Entities.User;
using Data.DartContext.Security;
using Logic.Bot.DI;
using Logic.Profile.DI;
using Logic.Shared.DI;
using Logic.Statistics.DI;
using Logic.Tournament.DI;
using Logic.Training.DI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Bot;
using Shared.Models.Options;

namespace Logic.Integration.Tests.Infrastructure
{
    /// <summary>
    /// Echte Services wie in Web.Core (AddSharedServices, AddBotServices, …) gegen eine frische MySQL-Datenbank.
    /// Ersetzt werden nur der angemeldete Benutzer (TestClaimsHelper) und der Zufall (FixedRandomSource).
    /// Jeder Scope entspricht einer HTTP-Anfrage (eigener DbContext).
    /// </summary>
    public sealed class TestHost : IAsyncDisposable
    {
        public const string TestPassword = "Integration-Test-1";

        private readonly ServiceProvider _provider;

        private TestHost(ServiceProvider provider)
        {
            _provider = provider;
        }

        public TestClaimsHelper Claims => _provider.GetRequiredService<TestClaimsHelper>();

        public static async Task<TestHost> CreateAsync(MySqlFixture fixture, Action<IServiceCollection>? configure = null)
        {
            var connectionString = fixture.CreateConnectionString();
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddDbContext<DartDatabaseContext>(options => options.UseMySQL(connectionString));
            services.AddOptions<DartBotOptions>();
            services.AddOptions<EmailOptions>();

            services.AddSharedServices();
            services.AddProfileServices();
            services.AddTrainingServices();
            services.AddBotServices();
            services.AddStatisticsServices();
            services.AddTournamentServices();

            // Ein Benutzer für alle Scopes des Tests, umschaltbar per SignIn.
            services.AddSingleton<TestClaimsHelper>();
            services.Replace(ServiceDescriptor.Scoped<IClaimsHelper>(sp => sp.GetRequiredService<TestClaimsHelper>()));
            services.Replace(ServiceDescriptor.Singleton<IRandomSource, FixedRandomSource>());

            configure?.Invoke(services);

            var host = new TestHost(services.BuildServiceProvider(validateScopes: true));

            // Migrationen wie beim API-Start (prüft damit auch die Migrationen selbst).
            await using (var scope = host.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<DartDatabaseContext>().Database.MigrateAsync();
            }

            return host;
        }

        public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

        /// <summary>Führt eine Aktion in einem eigenen Scope (= eigene Anfrage) aus.</summary>
        public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull
        {
            await using var scope = CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        }

        public async Task RunAsync<TService>(Func<TService, Task> action) where TService : notnull
        {
            await using var scope = CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<TService>());
        }

        /// <summary>Direkter Datenbankzugriff für Vorbereitung und Prüfungen.</summary>
        public async Task<TResult> DbAsync<TResult>(Func<DartDatabaseContext, Task<TResult>> action)
        {
            await using var scope = CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<DartDatabaseContext>());
        }

        /// <summary>Legt einen aktiven, bestätigten Benutzer mit Passwort TestPassword an.</summary>
        public Task<int> CreateUserAsync(string firstName = "Test", string lastName = "Spieler") =>
            DbAsync(async db =>
            {
                var user = new UserEntity
                {
                    FirstName = firstName,
                    LastName = lastName,
                    EmailAddress = $"{Guid.NewGuid():N}@test.local",
                    EmailVerifiedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "tests",
                    UserCredentials = new UserCredentialsEntity
                    {
                        PasswordHash = PasswordHasher.HashPassword(TestPassword),
                        PasswordChangedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "tests"
                    }
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                return user.Id;
            });

        /// <summary>Gibt einem Benutzer eine Rolle in der Datenbank (Rollen-Ids aus RoleSeed).</summary>
        public Task AssignRoleAsync(int userId, UserRoleEnum role) =>
            DbAsync(async db =>
            {
                var roleId = await db.Set<RoleEntity>().Where(r => r.UserRole == role).Select(r => r.Id).SingleAsync();
                db.Set<UserRoleEntity>().Add(new UserRoleEntity { UserId = userId, RoleId = roleId, CreatedAt = DateTime.UtcNow, CreatedBy = "tests" });
                return await db.SaveChangesAsync();
            });

        public async Task<int> SignInNewPlayerAsync()
        {
            var userId = await CreateUserAsync();
            Claims.SignIn(userId, UserRoleEnum.Player);
            return userId;
        }

        public ValueTask DisposeAsync() => _provider.DisposeAsync();
    }
}
