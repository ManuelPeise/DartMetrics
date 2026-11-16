using Logic.Bot.Administration;
using Logic.Bot.Games;
using Logic.Bot.Learning;
using Logic.Bot.Models;
using Logic.Bot.Play;
using Logic.Bot.Policy;
using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Bot;

namespace Logic.Bot.DI
{
    public static class BotServiceRegistration
    {
        public static void AddBotServices(this IServiceCollection services)
        {
            services.AddSingleton<IRandomSource, DefaultRandomSource>();
            services.AddSingleton<IThrowSimulator, ThrowSimulator>();
            // Wegebuch im Checkout-Bereich, sonst exakte Tabelle je Wurfprofil (Tabellen werden je Profil zwischengespeichert).
            services.AddSingleton<PolicyTableCache>();
            services.AddSingleton<ITargetSelector, ProTargetSelector>();
            services.AddSingleton<IBotVisitPlayer, BotVisitPlayer>();

            // Services mit Datenbank.
            services.AddScoped<IDartBotAdministration, DartBotAdministration>();
            services.AddScoped<IDartBotGameService, DartBotGameService>();
            services.AddScoped<IDartBotLearningService, DartBotLearningService>();
        }
    }
}
