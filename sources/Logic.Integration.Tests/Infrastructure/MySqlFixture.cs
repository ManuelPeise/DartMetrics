using MySql.Data.MySqlClient;
using Testcontainers.MySql;

namespace Logic.Integration.Tests.Infrastructure
{
    /// <summary>
    /// Ein MySQL-8.4-Container für den ganzen Testlauf (wie in docker-compose.yml).
    /// Jede Testklasse bekommt über CreateConnectionString eine eigene, frische Datenbank.
    /// Voraussetzung: Docker läuft.
    /// </summary>
    public sealed class MySqlFixture : IAsyncLifetime
    {
        private readonly MySqlContainer _container = new MySqlBuilder()
            .WithImage("mysql:8.4")
            .WithUsername("root")
            .WithPassword("integration-tests")
            .Build();

        public Task InitializeAsync() => _container.StartAsync();

        public Task DisposeAsync() => _container.DisposeAsync().AsTask();

        public string CreateConnectionString()
        {
            var builder = new MySqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Database = $"it_{Guid.NewGuid():N}"[..30]
            };

            return builder.ConnectionString;
        }
    }

    [CollectionDefinition(Name)]
    public sealed class MySqlCollection : ICollectionFixture<MySqlFixture>
    {
        public const string Name = "MySQL";
    }
}
