using Logic.Bot.Ml;
using Microsoft.ML;
using Microsoft.ML.Data;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Zielwahl über das Modell: alle Kandidaten der Situation bewerten, den mit den wenigsten erwarteten Darts wählen.
    /// Nicht threadsicher (PredictionEngine) – je Thread eine eigene Instanz.
    /// Vorläufig im Werkzeug für den Vergleich (Phase 4); in Phase 5 wandert die Zielwahl nach Logic.Bot.
    /// </summary>
    public sealed class MlTargetSelector : ITargetSelector
    {
        private readonly CandidateGenerator _generator = new();
        private readonly CandidateFeatureBuilder _features = new();
        private readonly PredictionEngine<TargetCandidateRow, DartsPrediction> _engine;

        public MlTargetSelector(MLContext ml, ITransformer model)
        {
            _engine = ml.Model.CreatePredictionEngine<TargetCandidateRow, DartsPrediction>(model);
        }

        public DartTarget SelectTarget(BotThrowDecisionContext context)
        {
            var situation = new BotSituation(
                context.Remaining, context.DartsAvailable, context.OpponentRemaining, context.Profile,
                context.InMode, context.OutMode, context.IsOpened);

            DartTarget? best = null;
            var bestDarts = float.MaxValue;

            foreach (var candidate in _generator.Generate(situation))
            {
                var predicted = _engine.Predict(ToRow(_features.Build(situation, candidate))).Score;

                if (predicted < bestDarts)
                {
                    bestDarts = predicted;
                    best = candidate;
                }
            }

            // Ohne Kandidaten (kommt bei gültigen Ständen nicht vor) auf das sicherste Ziel ausweichen.
            return best ?? DartTarget.Single(1);
        }

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
