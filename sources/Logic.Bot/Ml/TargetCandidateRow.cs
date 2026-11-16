using Microsoft.ML.Data;
using Shared.Models.Bot.Ml;
namespace Logic.Bot.Ml
{
    /// <summary>
    /// Eine CSV-Zeile des Datensatzes für ML.NET (Spaltenreihenfolge = CsvDatasetWriter.Header).
    /// Segment und Multiplikator werden als Text geladen und one-hot kodiert (Kategorien, keine Größen).
    /// </summary>
    public sealed class TargetCandidateRow
    {
        /// <summary>Merkmale eines Kandidaten als Modell-Eingabe (Label-Spalten bleiben leer).</summary>
        public static TargetCandidateRow From(TargetCandidateFeatures f) => new()
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
            LeftoverIsCheckable = f.LeftoverIsCheckable ? 1 : 0,
            LeftoverMinDarts = f.LeftoverMinDarts,
            LeftoverFinishableThisVisit = f.LeftoverFinishableThisVisit ? 1 : 0,
            HitProbability = f.HitProbability,
            BustProbability = f.BustProbability
        };

        [LoadColumn(0)] public float Remaining { get; set; }
        [LoadColumn(1)] public float DartsAvailable { get; set; }
        [LoadColumn(2)] public float OpponentRemaining { get; set; }
        [LoadColumn(3)] public float OutMode { get; set; }
        [LoadColumn(4)] public float IsOpened { get; set; }
        [LoadColumn(5)] public float ThrowPrecision { get; set; }
        [LoadColumn(6)] public float ThrowConsistency { get; set; }
        [LoadColumn(7)] public float TacticalIntelligence { get; set; }
        [LoadColumn(8)] public float PressureResistance { get; set; }
        [LoadColumn(9)] public float RiskTaking { get; set; }
        [LoadColumn(10)] public string CandidateSegment { get; set; } = "";
        [LoadColumn(11)] public string CandidateMultiplier { get; set; } = "";
        [LoadColumn(12)] public float CandidateScore { get; set; }
        [LoadColumn(13)] public float LeftoverIfHit { get; set; }
        [LoadColumn(14)] public float IsFinishingDart { get; set; }
        [LoadColumn(15)] public float LeftoverIsCheckable { get; set; }
        [LoadColumn(16)] public float ExpectedDartsToFinish { get; set; }
        // Nur für die Auswertung (Standardfehler des Labels), keine Merkmale.
        [LoadColumn(17)] public float DartsStdDev { get; set; }

        [LoadColumn(18)] public float Rollouts { get; set; }

        [LoadColumn(19)] public float SituationId { get; set; }
        [LoadColumn(20)] public float RelativeDarts { get; set; }

        // Schema 3 (hinten angehängt, damit die bisherigen Spalten ihre Nummer behalten)
        [LoadColumn(21)] public float LeftoverMinDarts { get; set; }

        [LoadColumn(22)] public float LeftoverFinishableThisVisit { get; set; }

        [LoadColumn(23)] public float HitProbability { get; set; }

        [LoadColumn(24)] public float BustProbability { get; set; }

        /// <summary>Numerische Merkmale (ohne die one-hot-kodierten Kategorien und ohne Label).</summary>
        public static readonly string[] NumericFeatures =
        [
            nameof(Remaining), nameof(DartsAvailable), nameof(OpponentRemaining), nameof(OutMode), nameof(IsOpened),
            nameof(ThrowPrecision), nameof(ThrowConsistency), nameof(TacticalIntelligence), nameof(PressureResistance), nameof(RiskTaking),
            nameof(CandidateScore), nameof(LeftoverIfHit), nameof(IsFinishingDart), nameof(LeftoverIsCheckable),
            nameof(LeftoverMinDarts), nameof(LeftoverFinishableThisVisit), nameof(HitProbability), nameof(BustProbability)
        ];
    }
}

namespace Logic.Bot.Ml
{
    /// <summary>Modellausgabe: vorhergesagte Darts mehr als der beste Kandidat (kleiner = besser).</summary>
    public sealed class DartsPrediction
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}
