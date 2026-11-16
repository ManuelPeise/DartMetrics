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
    }
}
