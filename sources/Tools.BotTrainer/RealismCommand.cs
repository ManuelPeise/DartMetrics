using System.Diagnostics;
using Logic.Bot.Administration;
using Logic.Bot.Learning;
using Logic.Bot.Models;
using Logic.Bot.Policy;
using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Logic.Shared.Bot;
using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Realismus-Prüfstand: je Stufe ein Bot wie bei der Erstellung (BotProfileFactory), Legs ab 501 mit Tagesform
    /// (neu je 5 Legs = ein „Spiel“) und Nerven. Kennzahlen wie in der Profi-Statistik.
    /// </summary>
    public static class RealismCommand
    {
        internal const int LegsPerGame = 5;

        private static readonly ParallelOptions Parallelism = new() { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

        public static int Run(Arguments arguments)
        {
            var legs = arguments.Int("legs", 2000);
            var cache = new PolicyTableCache(arguments.Int("samples", 4000));
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"Realismus: {legs} Legs je Stufe, 501 Double-Out, Bot wie bei der Erstellung (Seed fest)");
            Console.WriteLine();
            Console.WriteLine("  Stufe            Ziel-Ø   Ø      First-9  Checkout   Darts@D/Leg  100+/Leg  140+/Leg  180/Leg  1/5 bei T20");

            foreach (var (stage, target, name) in DartBotStages.All)
            {
                var profileEntity = BotProfileFactory.Create(target, null, new Random(BenchmarkCommand.Seed + (int)stage));
                var stats = new Stats[legs];
                var formSeries = Forms(legs, profileEntity.ThrowConsistency);

                Parallel.For(0, legs, Parallelism, i =>
                {
                    var seed = unchecked(BenchmarkCommand.Seed * 1_000_003 + i);
                    var random = new DefaultRandomSource(new Random(seed));
                    stats[i] = PlayLeg(profileEntity, BotForm.DayFactor(formSeries[i / LegsPerGame]), cache, random, new DefaultRandomSource(new Random(~seed)));
                });

                var darts = stats.Sum(s => s.Darts);
                var points = stats.Sum(s => s.Points);
                var first9Darts = stats.Sum(s => s.First9Darts);
                var first9Points = stats.Sum(s => s.First9Points);
                var doubleDarts = stats.Sum(s => s.DartsAtDouble);
                var doubleHits = stats.Sum(s => s.Checkouts);
                var t20 = stats.Sum(s => s.DartsAtT20);
                var stray = stats.Sum(s => s.StrayOneOrFive);

                Console.WriteLine($"  {name,-16} {target,6:0}  {points * 3.0 / darts,6:0.0}  {first9Points * 3.0 / first9Darts,7:0.0}  {doubleHits * 100.0 / Math.Max(1, doubleDarts),7:0.0} %" +
                                  $"  {doubleDarts / (double)legs,11:0.00}  {stats.Sum(s => s.Ton) / (double)legs,8:0.00}  {stats.Sum(s => s.Ton40) / (double)legs,8:0.00}" +
                                  $"  {stats.Sum(s => s.Max) / (double)legs,7:0.00}  {stray * 100.0 / Math.Max(1, t20),8:0.0} %");
            }

            Console.WriteLine();
            Console.WriteLine("Vergleich PDC-Spitze: Ø ~100, Checkout 40–50 %, Streuwürfe in 1/5 unter 5–10 %.");
            Console.WriteLine($"Dauer {stopwatch.Elapsed:hh\\:mm\\:ss}");
            return 0;
        }

        /// <summary>Formkurve über die „Spiele“ (je 5 Legs), wie im echten Spielstart.</summary>
        internal static float[] Forms(int legs, float consistency)
        {
            var random = new DefaultRandomSource(new Random(BenchmarkCommand.Seed));
            var forms = new float[legs / LegsPerGame + 1];
            var form = 0f;

            for (var g = 0; g < forms.Length; g++)
            {
                form = BotForm.NextForm(form, consistency, random);
                forms[g] = form;
            }

            return forms;
        }

        internal sealed class Stats
        {
            public int Darts, Points, First9Darts, First9Points, DartsAtDouble, Checkouts, Ton, Ton40, Max, DartsAtT20, StrayOneOrFive;
        }

        internal static Stats PlayLeg(Data.DartContext.Entities.Bot.DartBotProfileVersionEntity entity, float dayForm, PolicyTableCache cache,
            DefaultRandomSource throwRandom, DefaultRandomSource tacticRandom)
        {
            var rules = new X01Rules();
            var selector = new ProTargetSelector(cache, tacticRandom);
            var simulator = new ThrowSimulator(throwRandom);
            var stats = new Stats();
            var remaining = 501;
            var nerveEvents = 0;

            while (remaining > 0 && stats.Darts < 150)
            {
                var visitStart = remaining;
                var visitScore = 0;
                var profile = entity.ToSkillProfile(dayForm * (1f + entity.Nervousness * BotForm.NervesPerEvent * Math.Min(nerveEvents, BotForm.MaxNerveEvents)));
                var form = simulator.DrawVisitForm(profile);
                var board = new List<BoardPoint>();
                var bust = false;

                for (var dart = 0; dart < 3 && remaining > 0; dart++)
                {
                    var target = selector.SelectTarget(new BotThrowDecisionContext(remaining, 3 - dart, 501, profile, ScoreAtVisitStart: visitStart));
                    var atDouble = target.Score == remaining && target.IsDouble;
                    var aim = DartBoardGeometry.GetAimPoint(target);
                    var point = simulator.Throw(aim, profile, form, isPressureSituation: atDouble, board);
                    var hit = DartBoardGeometry.Resolve(point);
                    board.Add(point);
                    stats.Darts++;

                    if (target == DartTarget.Triple(20))
                    {
                        stats.DartsAtT20++;
                        stats.StrayOneOrFive += hit.Segment is 1 or 5 ? 1 : 0;
                    }

                    if (atDouble)
                    {
                        stats.DartsAtDouble++;
                    }

                    if (rules.IsCheckout(visitStart, visitScore + hit.Score, hit.Segment, hit.Multiplier, X01OutModeEnum.Double))
                    {
                        stats.Checkouts++;
                        visitScore += hit.Score;
                        remaining = 0;
                        break;
                    }

                    if (rules.IsBust(visitStart, visitScore + hit.Score, hit.Segment, hit.Multiplier, X01OutModeEnum.Double))
                    {
                        bust = true;
                        remaining = visitStart;
                        visitScore = 0;
                        break;
                    }

                    if (atDouble)
                    {
                        nerveEvents++;
                    }

                    visitScore += hit.Score;
                    remaining -= hit.Score;
                }

                nerveEvents = remaining == 0 ? 0 : nerveEvents + (bust ? 1 : 0);

                stats.Points += visitScore;
                if (stats.Darts <= 9)
                {
                    stats.First9Darts = stats.Darts;
                    stats.First9Points = stats.Points;
                }

                stats.Ton += visitScore >= 100 ? 1 : 0;
                stats.Ton40 += visitScore >= 140 ? 1 : 0;
                stats.Max += visitScore == 180 ? 1 : 0;
            }

            return stats;
        }
    }
}
