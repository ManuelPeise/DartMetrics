using System.Diagnostics;
using System.Globalization;
using Logic.Bot.Administration;
using Logic.Bot.Learning;
using Logic.Bot.Models;
using Logic.Bot.Policy;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Kalibrierung: welche Streuung (mm) braucht ein Bot, wie ihn die Erstellung anlegt (BotProfileFactory, Mittelwerte
    /// ohne Zufall: Wegewissen, Form, Nerven, schlechte Darts je Stärke), damit er im Mittel den Ziel-Ø spielt?
    /// Gespielt wird wie im Realismus-Prüfstand (Tagesform je 5 Legs, Nerven). Suche je Ziel-Ø per Bisektion.
    /// Die Ausgabe am Ende gehört in AverageCalibration.Points.
    /// </summary>
    public static class CalibrateCommand
    {
        private static readonly ParallelOptions Parallelism = new() { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

        public static int Run(Arguments arguments)
        {
            var legs = arguments.Int("legs", 1500);
            var steps = arguments.Int("steps", 9);
            var targets = arguments.Floats("averages", [15f, 20f, 25f, 30f, 35f, 40f, 47f, 55f, 62f, 70f, 77f, 85f, 90f, 95f, 100f, 105f, 110f, 115f]);
            var cache = new PolicyTableCache(arguments.Int("samples", 4000));
            var stopwatch = Stopwatch.StartNew();
            var points = new List<(float Sigma, double Average)>();

            Console.WriteLine($"Kalibrierung: {legs} Legs je Versuch, {steps} Schritte je Ziel-Ø, Bot wie bei der Erstellung (Mittelwerte)");

            foreach (var target in targets)
            {
                // log(σ) bisektieren: kleine Streuung = hoher Ø.
                var low = MathF.Log(2f);
                var high = MathF.Log(120f);
                var measured = 0.0;
                var sigma = 0f;

                for (var step = 0; step < steps; step++)
                {
                    sigma = MathF.Exp((low + high) / 2);
                    measured = Measure(target, sigma, legs, cache);

                    if (measured > target)
                    {
                        low = MathF.Log(sigma);
                    }
                    else
                    {
                        high = MathF.Log(sigma);
                    }
                }

                points.Add((sigma, measured));
                Console.WriteLine($"  Ziel Ø {target,5:0} → Streuung {sigma,6:0.00} mm (gemessen Ø {measured:0.0}, {stopwatch.Elapsed:mm\\:ss})");
            }

            Console.WriteLine();
            Console.WriteLine("Für AverageCalibration.Points (Streuung mm, Durchschnitt), absteigender Ø:");
            Console.WriteLine(string.Join(", ", points
                .OrderBy(p => p.Sigma)
                .Select(p => string.Create(CultureInfo.InvariantCulture, $"({p.Sigma:0.00}f, {p.Average:0.0}f)"))));
            return 0;
        }

        private static double Measure(float target, float sigma, int legs, PolicyTableCache cache)
        {
            var profile = BotProfileFactory.Create(target, null, new Random(1), randomize: false, sigmaOverride: sigma);
            var forms = RealismCommand.Forms(legs, profile.ThrowConsistency);
            var stats = new RealismCommand.Stats[legs];

            Parallel.For(0, legs, Parallelism, i =>
            {
                var seed = unchecked(BenchmarkCommand.Seed * 1_000_003 + i);
                stats[i] = RealismCommand.PlayLeg(profile, BotForm.DayFactor(forms[i / RealismCommand.LegsPerGame]), cache,
                    new DefaultRandomSource(new Random(seed)), new DefaultRandomSource(new Random(~seed)));
            });

            return stats.Sum(s => s.Points) * 3.0 / stats.Sum(s => s.Darts);
        }
    }
}
