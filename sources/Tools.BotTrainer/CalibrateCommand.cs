using System.Diagnostics;
using System.Globalization;
using Logic.Bot.Models;
using Logic.Bot.Policy;
using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Shared.Enums.X01;
using Shared.Models.Bot;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Kalibrierung: welche Streuung (mm) ergibt welchen 3-Dart-Durchschnitt? Gespielt wird mit der echten Zielwahl
    /// (Wegebuch + Tabelle) und einem typischen V2-Profil. Die Ausgabe am Ende ist die Tabelle für die Bot-Erstellung.
    /// </summary>
    public static class CalibrateCommand
    {
        private static readonly ParallelOptions Parallelism = new() { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

        public static int Run(Arguments arguments)
        {
            var legs = arguments.Int("legs", 2000);
            var sigmas = arguments.Floats("sigmas", [3f, 4f, 5f, 6f, 7f, 8f, 10f, 12f, 15f, 18f, 22f, 27f, 33f, 40f, 50f, 60f, 75f]);
            var cache = new PolicyTableCache(arguments.Int("samples", 4000));
            var stopwatch = Stopwatch.StartNew();
            var rows = new List<(float Sigma, double Average, double DartsPerLeg)>();

            Console.WriteLine($"Kalibrierung: {legs} Legs je Streuung, 501 Double-Out, Zielwahl Wegebuch + Tabelle");

            foreach (var sigma in sigmas)
            {
                var profile = TypicalProfile(sigma);
                var situation = new BotSituation(501, 3, 501, profile, X01InModeEnum.Straight, X01OutModeEnum.Double);
                var darts = new int[legs];

                Parallel.For(0, legs, Parallelism, i =>
                {
                    var legSeed = unchecked(BenchmarkCommand.Seed * 1_000_003 + i);
                    var strategy = new ProTargetSelector(cache, new DefaultRandomSource(new Random(~legSeed)));
                    var simulator = new LegRolloutSimulator(strategy, new ThrowSimulator(new DefaultRandomSource(new Random(legSeed))));
                    var first = strategy.SelectTarget(new BotThrowDecisionContext(501, 3, 501, profile));
                    darts[i] = simulator.Play(situation, first, 150);
                });

                var perLeg = darts.Average();
                var average = 501.0 * 3 / perLeg;
                rows.Add((sigma, average, perLeg));
                Console.WriteLine($"  Streuung {sigma,5:0.#} mm → Ø {average,6:0.0}  ({perLeg:0.00} Darts pro Leg, {stopwatch.Elapsed:mm\\:ss})");
            }

            Console.WriteLine();
            Console.WriteLine("Für AverageCalibration.Points (Streuung mm, Durchschnitt):");
            Console.WriteLine(string.Join(", ", rows.Select(r =>
                string.Create(CultureInfo.InvariantCulture, $"({r.Sigma:0.#}f, {r.Average:0.0}f)"))));
            return 0;
        }

        /// <summary>Typischer Bot: senkrecht etwas stärker, wenige schlechte Darts, solide Form und Nerven, kennt die Wege.</summary>
        public static BotSkillProfile TypicalProfile(float sigma) => new(0.9f, 0.8f, 1f, 0.8f, 0.5f)
        {
            SigmaMm = sigma,
            VerticalRatio = 1.3f,
            BadDartRate = 0.03f
        };
    }
}
