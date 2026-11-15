using Microsoft.ML.Data;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Eine CSV-Zeile des Datensatzes für ML.NET (Spaltenreihenfolge = CsvDatasetWriter.Header).
    /// Segment und Multiplikator werden als Text geladen und one-hot kodiert (Kategorien, keine Größen).
    /// </summary>
    public sealed class TargetCandidateRow
    {
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

        /// <summary>Numerische Merkmale (ohne die one-hot-kodierten Kategorien und ohne Label).</summary>
        public static readonly string[] NumericFeatures =
        [
            nameof(Remaining), nameof(DartsAvailable), nameof(OpponentRemaining), nameof(OutMode), nameof(IsOpened),
            nameof(ThrowPrecision), nameof(ThrowConsistency), nameof(TacticalIntelligence), nameof(PressureResistance), nameof(RiskTaking),
            nameof(CandidateScore), nameof(LeftoverIfHit), nameof(IsFinishingDart), nameof(LeftoverIsCheckable)
        ];
    }
}
