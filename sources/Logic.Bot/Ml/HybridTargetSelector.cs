using Shared.Interfaces.Bot;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Ml
{
    /// <summary>
    /// Zielwahl mit Modell (Phase 5): das Regelwerk schlägt ein Ziel vor; das Modell bewertet alle Kandidaten und
    /// ersetzt den Vorschlag nur, wenn sein bester Kandidat laut Vorhersage um mehr als <c>margin</c> Darts besser ist.
    /// Im Vergleich (Modell 3, Schwelle 0,3) 0,2–0,5 Darts pro Leg weniger als das Regelwerk auf allen Stufen 0,5–0,95.
    /// </summary>
    public sealed class HybridTargetSelector : ITargetSelector
    {
        private readonly CandidateGenerator _generator = new();
        private readonly CandidateFeatureBuilder _features = new();
        private readonly LoadedTargetModel _model;
        private readonly ITargetSelector _rules;
        private readonly float _margin;
        private int _decisions;
        private int _overrides;

        public HybridTargetSelector(LoadedTargetModel model, ITargetSelector rules, float margin)
        {
            _model = model;
            _rules = rules;
            _margin = Math.Max(0f, margin);
        }

        /// <summary>Entscheidungen bzw. Abweichungen vom Regelwerk (für Auswertungen).</summary>
        public int Decisions => _decisions;

        public int Overrides => _overrides;

        public DartTarget SelectTarget(BotThrowDecisionContext context)
        {
            var ruleTarget = _rules.SelectTarget(context);
            Interlocked.Increment(ref _decisions);

            var situation = new BotSituation(
                context.Remaining, context.DartsAvailable, context.OpponentRemaining, context.Profile,
                context.InMode, context.OutMode, context.IsOpened);

            DartTarget? best = null;
            var bestScore = float.MaxValue;
            float? ruleScore = null;

            foreach (var candidate in _generator.Generate(situation))
            {
                var score = Predict(situation, candidate);

                if (candidate == ruleTarget)
                {
                    ruleScore = score;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best is null)
            {
                return ruleTarget;
            }

            ruleScore ??= Predict(situation, ruleTarget);

            if (best.Value != ruleTarget && bestScore + _margin < ruleScore.Value)
            {
                Interlocked.Increment(ref _overrides);
                return best.Value;
            }

            return ruleTarget;
        }

        private float Predict(BotSituation situation, DartTarget candidate) =>
            _model.Predict(TargetCandidateRow.From(_features.Build(situation, candidate)));
    }
}
