using Logic.Tournament.Knockout;
using Logic.Tournament.League;
using Logic.Tournament.Play;
using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Tournament;

namespace Logic.Tournament.DI
{
    public static class TournamentServiceRegistration
    {
        public static void AddTournamentServices(this IServiceCollection services)
        {
            services.AddScoped<ILeagueService, LeagueService>();
            services.AddScoped<IKnockoutService, KnockoutService>();
            services.AddScoped<ITournamentMatchService, TournamentMatchService>();

            // Bot gegen Bot zur geplanten Zeit (Abschnitt "Tournament", siehe TournamentOptions).
            services.AddHostedService<BotMatchWorker>();
        }
    }
}
