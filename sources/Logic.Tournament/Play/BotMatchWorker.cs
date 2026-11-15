using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Interfaces.Tournament;
using Shared.Models.Options;

namespace Logic.Tournament.Play
{
    /// <summary>
    /// Hintergrunddienst: startet Bot-gegen-Bot-Matches erst zum Termin ihrer Runde und spielt je Takt eine Aufnahme pro Match,
    /// damit man sie live verfolgen kann. Nichts wird vorab berechnet.
    /// </summary>
    public sealed class BotMatchWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TournamentOptions _options;
        private readonly ILogger<BotMatchWorker> _logger;

        public BotMatchWorker(IServiceScopeFactory scopeFactory, IOptions<TournamentOptions> options, ILogger<BotMatchWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.BotMatchesEnabled)
            {
                _logger.LogInformation("Bot-gegen-Bot-Matches sind ausgeschaltet (Tournament:BotMatchesEnabled).");
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.BotVisitIntervalSeconds));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // Jeder Takt = eigener Scope (eigener DbContext), wie eine Anfrage.
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITournamentMatchService>();
                    await service.PlayDueBotMatchesAsync(DateTime.UtcNow);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(exception, "Bot-gegen-Bot-Takt fehlgeschlagen.");
                }
            }
        }
    }
}
