using Logic.Tournament.League;
using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Tournament;

namespace Logic.Tournament.DI
{
    public static class TournamentServiceRegistration
    {
        public static void AddTournamentServices(this IServiceCollection services)
        {
            services.AddScoped<ILeagueService, LeagueService>();
        }
    }
}
