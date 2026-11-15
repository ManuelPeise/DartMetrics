using Microsoft.Extensions.DependencyInjection;

namespace Logic.Tournament.DI
{
    public static class TournamentServiceRegistration
    {
        /// <summary>Phase 1: nur Datenmodell und <c>X01Match</c> (ohne DI). Services folgen ab Phase 2 (Liga).</summary>
        public static void AddTournamentServices(this IServiceCollection services)
        {
        }
    }
}
