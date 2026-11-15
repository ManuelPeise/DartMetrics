using Logic.Bot.Models;
using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Ml
{
    /// <summary>
    /// Bewertet alle Kandidaten einer Situation: je Kandidat N Rollouts mit der Regelstrategie,
    /// Mittelwert der Darts = Label ExpectedDartsToFinish. Kandidaten laufen parallel, jeder mit eigenem
    /// Zufallsstrom aus Seed und Index – das Ergebnis ist unabhängig von der Parallelisierung reproduzierbar.
    /// </summary>
    public sealed class CandidateEvaluator
    {
        private readonly CandidateGenerator _generator = new();
        private readonly CandidateFeatureBuilder _featureBuilder = new();
        private readonly Func<int, IRandomSource> _randomFactory;

        /// <param name="randomFactory">Zufallsquelle je Strom (Standard: System.Random mit diesem Seed); in Tests austauschbar.</param>
        public CandidateEvaluator(Func<int, IRandomSource>? randomFactory = null)
        {
            _randomFactory = randomFactory ?? (seed => new DefaultRandomSource(new Random(seed)));
        }

        public IReadOnlyList<TargetCandidateFeatures> Evaluate(BotSituation situation, RolloutOptions options)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(options.RolloutsPerCandidate, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxDartsPerLeg, 1);

            var candidates = _generator.Generate(situation);
            var results = new TargetCandidateFeatures[candidates.Count];

            Parallel.For(0, candidates.Count, index =>
            {
                results[index] = EvaluateCandidate(situation, candidates[index], options, StreamSeed(options.Seed, index));
            });

            return results;
        }

        public TargetCandidateFeatures EvaluateCandidate(BotSituation situation, DartTarget candidate, RolloutOptions options, int seed)
        {
            var random = _randomFactory(seed);
            var simulator = new LegRolloutSimulator(new RuleBasedTargetSelector(random), new ThrowSimulator(random));
            var darts = new int[options.RolloutsPerCandidate];

            for (var i = 0; i < darts.Length; i++)
            {
                darts[i] = simulator.Play(situation, candidate, options.MaxDartsPerLeg);
            }

            var mean = darts.Average();
            var variance = darts.Sum(d => (d - mean) * (d - mean)) / darts.Length;

            var features = _featureBuilder.Build(situation, candidate);
            features.ExpectedDartsToFinish = (float)Math.Round(mean, 2);
            features.DartsStdDev = (float)Math.Round(Math.Sqrt(variance), 2);
            features.Rollouts = darts.Length;

            return features;
        }

        /// <summary>Fester, gut gestreuter Seed je Kandidat (unabhängig von der Thread-Reihenfolge).</summary>
        private static int StreamSeed(int seed, int index) => unchecked(seed * 486187739 + index * 16777619);
    }
}
