using Data.DartContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models.Options;

namespace Logic.Administration.DataRetention
{
    /// <summary>
    /// Hintergrunddienst für die Löschfristen (Datensparsamkeit):
    /// - nicht bestätigte Registrierungen nach <see cref="DataRetentionOptions.UnverifiedAccountDays"/> Tagen,
    /// - Log-Einträge nach <see cref="DataRetentionOptions.LogMessageDays"/> Tagen.
    /// Läuft einmal kurz nach dem Start und danach im eingestellten Intervall.
    /// </summary>
    public sealed class DataRetentionWorker : BackgroundService
    {
        private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DataRetentionOptions _options;
        private readonly ILogger<DataRetentionWorker> _logger;

        public DataRetentionWorker(IServiceScopeFactory scopeFactory, IOptions<DataRetentionOptions> options, ILogger<DataRetentionWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Automatische Löschfristen sind ausgeschaltet (DataRetention:Enabled).");
                return;
            }

            try
            {
                // Erst nach dem Start (Migrationen, Seed) loslegen.
                await Task.Delay(StartDelay, stoppingToken);

                using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.IntervalHours));

                do
                {
                    await RunOnceSafeAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Anwendung wird beendet.
            }
        }

        private async Task RunOnceSafeAsync(CancellationToken stoppingToken)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DartDatabaseContext>();

                var (accounts, logs) = await RunOnceAsync(dbContext, _options, DateTime.UtcNow, stoppingToken);

                if (accounts > 0 || logs > 0)
                {
                    _logger.LogInformation("Löschfristen: {Accounts} unbestätigte Konten und {Logs} Log-Einträge gelöscht.", accounts, logs);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Bereinigung (Löschfristen) fehlgeschlagen.");
            }
        }

        /// <summary>
        /// Eine Bereinigung. Öffentlich und ohne Timer, damit sie sich direkt testen lässt.
        /// Gibt die Anzahl gelöschter Konten und Log-Einträge zurück.
        /// </summary>
        public static async Task<(int Accounts, int Logs)> RunOnceAsync(DartDatabaseContext dbContext, DataRetentionOptions options, DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            var accountCutoff = utcNow.AddDays(-options.UnverifiedAccountDays);
            var logCutoff = utcNow.AddDays(-options.LogMessageDays);

            // Nie bestätigt, nicht anonymisiert (gelöschte Konten haben DeletedAt) und seit der Frist unverändert
            // (eine erneute Registrierung setzt UpdatedAt). Konten mit Spieldaten bleiben sicherheitshalber stehen.
            // Anmeldedaten, Rollen und Profilbild löscht die Datenbank per Cascade mit.
            var accounts = await dbContext.Users
                .Where(u => u.EmailVerifiedAt == null
                            && u.DeletedAt == null
                            && (u.UpdatedAt ?? u.CreatedAt) < accountCutoff
                            && !dbContext.TrainingSessions.Any(t => t.UserId == u.Id)
                            && !dbContext.TournamentParticipants.Any(p => p.UserId == u.Id)
                            && !dbContext.DartBotGamePlayers.Any(p => p.UserId == u.Id))
                .ExecuteDeleteAsync(cancellationToken);

            var logs = await dbContext.LogMessages
                .Where(l => l.CreatedAt < logCutoff)
                .ExecuteDeleteAsync(cancellationToken);

            return (accounts, logs);
        }
    }
}
