using System.Diagnostics;
using Logic.Bot.Models;
using Logic.Bot.Policy;
using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Shared.Enums.X01;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Prüfstand: Regelwerk gegen Tabellen-Zielwahl bei gleichen Würfen (gepaarter Vergleich), je Streuung (mm).
    /// Fester Seed, Legs ab 501 ohne Gegner.
    /// </summary>
    public static class BenchmarkCommand
    {
        public const int Seed = 20261116;

        private static readonly ParallelOptions Parallelism = new() { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

        public static int Run(Arguments arguments)
        {
            var legs = arguments.Int("legs", 2000);
            var maxDarts = arguments.Int("max-darts", 90);
            var outMode = arguments.Flag("straight-out") ? X01OutModeEnum.Straight : X01OutModeEnum.Double;
            var sigmas = arguments.Floats("sigmas", [6f, 9f, 13f, 20f, 30f]);
            var tolerances = arguments.Floats("tolerances", [0f, 0.05f, 0.2f]);
            var cache = new PolicyTableCache(arguments.Int("samples", 4000));
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"Prüfstand: {legs} Legs je Stufe, 501 {(outMode == X01OutModeEnum.Straight ? "Straight-Out" : "Double-Out")}, Seed {Seed}");
            Console.WriteLine("Differenz < 0 = weniger Darts als das Regelwerk. 95-%-Intervall in Klammern.");

            foreach (var sigma in sigmas)
            {
                var profile = new BotSkillProfile(0.9f, 0.8f, 1f, 0.8f, 0.5f) { SigmaMm = sigma, VerticalRatio = 1.3f, BadDartRate = 0.03f };
                var situation = new BotSituation(501, 3, 501, profile, X01InModeEnum.Straight, outMode);

                var build = Stopwatch.StartNew();
                var table = cache.Get(profile, outMode, opponentPressure: false);
                Console.WriteLine();
                Console.WriteLine($"Streuung {sigma:0.#} mm: Tabelle in {build.Elapsed.TotalSeconds:0.0} s, erwartet {table.ExpectedDarts(501):0.00} Darts");

                var rules = Play(legs, situation, maxDarts, seed => new RuleBasedTargetSelector(new DefaultRandomSource(new Random(~seed))));
                Console.WriteLine($"  Regelwerk          {rules.Average(),6:0.00} Darts  (Ø {Average(rules):0.0})");

                foreach (var tolerance in tolerances)
                {
                    var darts = Play(legs, situation, maxDarts, seed =>
                    {
                        var random = new DefaultRandomSource(new Random(~seed));
                        return new PolicyTableTargetSelector(cache, new RuleBasedTargetSelector(random), random, tolerance);
                    });

                    Report($"Tabelle ±{tolerance:0.00}", darts, rules);
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Dauer {stopwatch.Elapsed:hh\\:mm\\:ss}");
            return 0;
        }

        /// <summary>3-Dart-Durchschnitt aus Darts pro Leg (501 Punkte je Leg).</summary>
        private static double Average(int[] darts) => 501.0 * 3 / darts.Average();

        private static int[] Play(int legs, BotSituation situation, int maxDarts, Func<int, ITargetSelector> selector)
        {
            var darts = new int[legs];

            Parallel.For(0, legs, Parallelism, i =>
            {
                var legSeed = unchecked(Seed * 1_000_003 + i);
                var strategy = selector(legSeed);
                var simulator = new LegRolloutSimulator(strategy, new ThrowSimulator(new DefaultRandomSource(new Random(legSeed))));
                var first = strategy.SelectTarget(new BotThrowDecisionContext(
                    situation.Remaining, situation.DartsAvailable, situation.OpponentRemaining, situation.Profile, situation.InMode, situation.OutMode, situation.Opened));
                darts[i] = simulator.Play(situation, first, maxDarts);
            });

            return darts;
        }

        private static void Report(string label, int[] darts, int[] rules)
        {
            var differences = darts.Zip(rules, (a, b) => (double)(a - b)).ToArray();
            var mean = differences.Average();
            var standardError = Math.Sqrt(differences.Sum(d => (d - mean) * (d - mean)) / (differences.Length - 1) / differences.Length);

            Console.WriteLine($"  {label,-18} {darts.Average(),6:0.00} Darts  (Ø {Average(darts):0.0})   {mean,6:+0.00;-0.00} [{mean - 1.96 * standardError:+0.00;-0.00}; {mean + 1.96 * standardError:+0.00;-0.00}]");
        }
    }
}
