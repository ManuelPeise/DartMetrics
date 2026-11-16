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
    /// Mittelwert der Darts = ExpectedDartsToFinish, Abstand zum besten Kandidaten = RelativeDarts.
    /// Gemeinsamer Zufall: alle Kandidaten einer Situation werfen mit demselben Zufallsstrom (Rollout i hat
    /// für jeden Kandidaten dieselben Würfe), damit der Unterschied zwischen Kandidaten nur von der Zielwahl kommt.
    /// Würfe und Strategie haben getrennte Ströme, damit Taktikfehler die Würfe nicht verschieben.
    /// </summary>
    public sealed class CandidateEvaluator
    {
        private readonly CandidateGenerator _generator = new();
        private readonly CandidateFeatureBuilder _featureBuilder = new();
        private readonly Func<int, IRandomSource> _randomFactory;
        private readonly Func<IRandomSource, ITargetSelector> _policyFactory;

        /// <param name="randomFactory">Zufallsquelle je Strom (Standard: System.Random mit diesem Seed); in Tests austauschbar.</param>
        /// <param name="policyFactory">
        /// Strategie für die Darts nach dem bewerteten Kandidaten (Standard: Regelwerk). Mit einem Modell (HybridTargetSelector)
        /// lernt die nächste Runde, welcher Dart am besten ist, wenn danach das Modell weiterspielt.
        /// </param>
        public CandidateEvaluator(Func<int, IRandomSource>? randomFactory = null, Func<IRandomSource, ITargetSelector>? policyFactory = null)
        {
            _randomFactory = randomFactory ?? (seed => new DefaultRandomSource(new Random(seed)));
            _policyFactory = policyFactory ?? (random => new RuleBasedTargetSelector(random));
        }

        public IReadOnlyList<TargetCandidateFeatures> Evaluate(BotSituation situation, RolloutOptions options)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(options.RolloutsPerCandidate, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxDartsPerLeg, 1);

            var candidates = _generator.Generate(situation);
            var results = new TargetCandidateFeatures[candidates.Count];

            Parallel.For(0, candidates.Count, index =>
            {
                results[index] = EvaluateCandidate(situation, candidates[index], options, options.Seed);
            });

            if (results.Length > 0)
            {
                var best = results.Min(r => r.ExpectedDartsToFinish);

                foreach (var row in results)
                {
                    row.RelativeDarts = (float)Math.Round(row.ExpectedDartsToFinish - best, 2);
                }
            }

            return results;
        }

        public TargetCandidateFeatures EvaluateCandidate(BotSituation situation, DartTarget candidate, RolloutOptions options, int seed)
        {
            var darts = new int[options.RolloutsFor(situation.Remaining)];

            for (var i = 0; i < darts.Length; i++)
            {
                // Je Rollout ein eigener Seed: Rollout i startet für jeden Kandidaten mit denselben Würfen.
                var rolloutSeed = unchecked(seed * 486187739 + i * 16777619);
                var simulator = new LegRolloutSimulator(
                    _policyFactory(_randomFactory(~rolloutSeed)),
                    new ThrowSimulator(_randomFactory(rolloutSeed)));

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
    }
}
