using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    /// <summary>Trainingsauftrag für die Admin-Seite "ML-Modelle".</summary>
    public class DartBotTrainingJobListItem
    {
        public int Id { get; set; }
        public DartBotTrainingJobPresetEnum Preset { get; set; }
        public DartBotTrainingJobStatusEnum Status { get; set; }
        public DartBotTrainingJobPhaseEnum? Phase { get; set; }
        public int ProgressPercent { get; set; }
        public string? ProgressText { get; set; }
        public int Situations { get; set; }
        public int CompareLegs { get; set; }
        public string RequestedBy { get; set; } = "";
        public bool CancelRequested { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int? DatasetId { get; set; }
        public int? ModelId { get; set; }
        public int? ModelVersionNumber { get; set; }
        public bool? ComparisonPassed { get; set; }

        /// <summary>Runde: Basis-Modell der Rollouts (null = Regelwerk).</summary>
        public int? BaseModelId { get; set; }
        public int? BaseModelVersionNumber { get; set; }

        /// <summary>Prüfstand des neuen Modells bzw. der Basis (Darts pro Leg ggü. Regelwerk, negativ = besser).</summary>
        public double? BenchmarkScore { get; set; }
        public double? BaseBenchmarkScore { get; set; }

        /// <summary>Nur bei Runden: neues Modell im Prüfstand besser als die Basis.</summary>
        public bool? BetterThanBase => BaseModelId is null || BenchmarkScore is null || BaseBenchmarkScore is null
            ? null
            : BenchmarkScore < BaseBenchmarkScore;
    }
}
