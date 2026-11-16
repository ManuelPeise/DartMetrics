using System.Collections.Concurrent;
using Shared.Enums.X01;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Ml
{
    /// <summary>
    /// Zielwahl mit Modell (Phase 5): das Regelwerk schlägt ein Ziel vor; das Modell bewertet alle Kandidaten und
    /// ersetzt den Vorschlag nur, wenn sein bester Kandidat laut Vorhersage um mehr als <c>margin</c> Darts besser ist.
    /// Für Rollouts mit Modell (Training in Runden) lassen sich die Vorhersagen je Zustand zwischenspeichern und das
    /// Modell auf den Finish-Bereich beschränken; im Spiel bleibt beides aus.
    /// </summary>
    public sealed class HybridTargetSelector : ITargetSelector
    {
        private readonly CandidateGenerator _generator = new();
        private readonly CandidateFeatureBuilder _features = new();
        private readonly LoadedTargetModel _model;
        private readonly ITargetSelector _rules;
        private readonly float _margin;
        private readonly ConcurrentDictionary<StateKey, IReadOnlyDictionary<DartTarget, float>>? _cache;
        private readonly int? _modelMaxRemaining;
        private int _decisions;
        private int _overrides;

        /// <param name="cache">Gemeinsamer Speicher der Vorhersagen je Zustand (z. B. je Situation beim Erzeugen eines Datensatzes).</param>
        /// <param name="modelMaxRemaining">Nur bis zu diesem Rest entscheidet das Modell mit, darüber allein das Regelwerk.</param>
        public HybridTargetSelector(
            LoadedTargetModel model,
            ITargetSelector rules,
            float margin,
            ConcurrentDictionary<StateKey, IReadOnlyDictionary<DartTarget, float>>? cache = null,
            int? modelMaxRemaining = null)
        {
            _model = model;
            _rules = rules;
            _margin = Math.Max(0f, margin);
            _cache = cache;
            _modelMaxRemaining = modelMaxRemaining;
        }

        /// <summary>Entscheidungen bzw. Abweichungen vom Regelwerk (für Auswertungen).</summary>
        public int Decisions => _decisions;

        public int Overrides => _overrides;

        public DartTarget SelectTarget(BotThrowDecisionContext context)
        {
            var ruleTarget = _rules.SelectTarget(context);
            Interlocked.Increment(ref _decisions);

            if (_modelMaxRemaining is not null && context.Remaining > _modelMaxRemaining.Value)
            {
                return ruleTarget;
            }

            var situation = new BotSituation(
                context.Remaining, context.DartsAvailable, context.OpponentRemaining, context.Profile,
                context.InMode, context.OutMode, context.IsOpened);

            var scores = _cache is null
                ? Score(situation)
                : _cache.GetOrAdd(StateKey.Of(context), _ => Score(situation));

            if (scores.Count == 0)
            {
                return ruleTarget;
            }

            DartTarget best = default;
            var bestScore = float.MaxValue;

            foreach (var (candidate, score) in scores)
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            var ruleScore = scores.TryGetValue(ruleTarget, out var known) ? known : Predict(situation, ruleTarget);

            if (best != ruleTarget && bestScore + _margin < ruleScore)
            {
                Interlocked.Increment(ref _overrides);
                return best;
            }

            return ruleTarget;
        }

        private IReadOnlyDictionary<DartTarget, float> Score(BotSituation situation)
        {
            var scores = new Dictionary<DartTarget, float>();

            foreach (var candidate in _generator.Generate(situation))
            {
                scores[candidate] = Predict(situation, candidate);
            }

            return scores;
        }

        private float Predict(BotSituation situation, DartTarget candidate) =>
            _model.Predict(TargetCandidateRow.From(_features.Build(situation, candidate)));

        /// <summary>Alles, was die Merkmale bestimmt (die Vorhersage hängt nur davon ab).</summary>
        public readonly record struct StateKey(
            int Remaining,
            int DartsAvailable,
            int OpponentRemaining,
            X01InModeEnum InMode,
            X01OutModeEnum OutMode,
            bool IsOpened,
            BotSkillProfile Profile)
        {
            public static StateKey Of(BotThrowDecisionContext context) => new(
                context.Remaining, context.DartsAvailable, context.OpponentRemaining,
                context.InMode, context.OutMode, context.IsOpened, context.Profile);
        }
    }
}
