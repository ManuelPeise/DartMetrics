using Logic.Bot.Ml;
using Microsoft.ML;
using Microsoft.ML.Data;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Hybrid-Zielwahl: das Regelwerk schlägt ein Ziel vor; das Modell bewertet alle Kandidaten und ersetzt den
    /// Vorschlag nur, wenn sein bester Kandidat laut Vorhersage um mehr als <c>margin</c> Darts besser ist.
    /// margin 0 = reines Modell. So kann das Modell das Regelwerk nur dort ändern, wo es sich deutlich sicher ist.
    /// Nicht threadsicher (PredictionEngine) – je Thread eine eigene Engine.
    /// Vorläufig im Werkzeug (Phase 4); in Phase 5 wandert die Zielwahl nach Logic.Bot.
    /// </summary>
    public sealed class MlTargetSelector : ITargetSelector
    {
        private readonly CandidateGenerator _generator = new();
        private readonly CandidateFeatureBuilder _features = new();
        private readonly PredictionEngine<TargetCandidateRow, DartsPrediction> _engine;
        private readonly ITargetSelector _rules;
        private readonly float _margin;

        public MlTargetSelector(PredictionEngine<TargetCandidateRow, DartsPrediction> engine, ITargetSelector rules, float margin)
        {
            _engine = engine;
            _rules = rules;
            _margin = margin;
        }

        /// <summary>Wie oft das Modell vom Regelwerk abgewichen ist (für die Auswertung).</summary>
        public int Decisions { get; private set; }

        public int Overrides { get; private set; }

        public static PredictionEngine<TargetCandidateRow, DartsPrediction> CreateEngine(MLContext ml, ITransformer model) =>
            ml.Model.CreatePredictionEngine<TargetCandidateRow, DartsPrediction>(model);

        public DartTarget SelectTarget(BotThrowDecisionContext context)
        {
            var ruleTarget = _rules.SelectTarget(context);
            Decisions++;

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

            // Regel-Ziel nicht unter den Kandidaten (z. B. sicherer Bust laut Generator): trotzdem bewerten.
            ruleScore ??= Predict(situation, ruleTarget);

            if (best.Value != ruleTarget && bestScore + _margin < ruleScore.Value)
            {
                Overrides++;
                return best.Value;
            }

            return ruleTarget;
        }

        private float Predict(BotSituation situation, DartTarget candidate) =>
            _engine.Predict(ToRow(_features.Build(situation, candidate))).Score;

        public static TargetCandidateRow ToRow(TargetCandidateFeatures f) => new()
        {
            Remaining = f.Remaining,
            DartsAvailable = f.DartsAvailable,
            OpponentRemaining = f.OpponentRemaining,
            OutMode = f.OutMode,
            IsOpened = f.IsOpened ? 1 : 0,
            ThrowPrecision = f.ThrowPrecision,
            ThrowConsistency = f.ThrowConsistency,
            TacticalIntelligence = f.TacticalIntelligence,
            PressureResistance = f.PressureResistance,
            RiskTaking = f.RiskTaking,
            CandidateSegment = f.CandidateSegment.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CandidateMultiplier = f.CandidateMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CandidateScore = f.CandidateScore,
            LeftoverIfHit = f.LeftoverIfHit,
            IsFinishingDart = f.IsFinishingDart ? 1 : 0,
            LeftoverIsCheckable = f.LeftoverIsCheckable ? 1 : 0
        };
    }

    public sealed class DartsPrediction
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}
