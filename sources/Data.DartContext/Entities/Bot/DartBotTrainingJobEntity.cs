using Shared.Enums.Bot;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Auftrag "Modell trainieren" aus dem Admin-Bereich. Der BotTrainer-Worker holt Aufträge ab und führt
    /// generate → train → compare aus. Aktiviert wird nie automatisch.
    /// </summary>
    public class DartBotTrainingJobEntity : AEntityBase
    {
        public DartBotTrainingJobPresetEnum Preset { get; set; }

        public DartBotTrainingJobStatusEnum Status { get; set; } = DartBotTrainingJobStatusEnum.Queued;

        public DartBotTrainingJobPhaseEnum? Phase { get; set; }

        /// <summary>Fortschritt 0–100 über alle Schritte.</summary>
        public int ProgressPercent { get; set; }

        public string? ProgressText { get; set; }

        // Parameter (beim Anlegen aus dem Preset übernommen)
        public int Situations { get; set; }

        public int RolloutsPerCandidate { get; set; }

        public int CompareLegs { get; set; }

        public int Seed { get; set; }

        /// <summary>Runde: in den Rollouts spielt dieses Modell weiter statt des Regelwerks (null = Regelwerk).</summary>
        public int? PolicyModelId { get; set; }

        public DartBotModelEntity? PolicyModel { get; set; }

        public string RequestedBy { get; set; } = null!;

        public bool CancelRequested { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        /// <summary>Lebenszeichen des Workers; bleibt es aus, gilt der Auftrag als abgebrochen.</summary>
        public DateTime? HeartbeatAt { get; set; }

        public string? ErrorMessage { get; set; }

        public int? DatasetId { get; set; }

        public DartBotTrainingDatasetEntity? Dataset { get; set; }

        public int? ModelId { get; set; }

        public DartBotModelEntity? Model { get; set; }

        /// <summary>Ergebnis des Vergleichs: auf keiner Skill-Stufe signifikant schlechter als das Regelwerk (mit mindestens einer Schwelle).</summary>
        public bool? ComparisonPassed { get; set; }
    }
}
