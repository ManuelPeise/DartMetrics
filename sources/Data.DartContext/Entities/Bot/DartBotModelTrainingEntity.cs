using Shared.Enums.Bot;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>Ein ML.NET-Trainingslauf. Ein fehlgeschlagener Lauf existiert ohne Modell.</summary>
    public class DartBotModelTrainingEntity : AEntityBase
    {
        public DartBotModelTypeEnum ModelType { get; set; }

        public int DatasetId { get; set; }

        public DartBotTrainingDatasetEntity Dataset { get; set; } = null!;

        public DartBotModelTrainingStatusEnum Status { get; set; } = DartBotModelTrainingStatusEnum.Pending;

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public long TrainingDataCount { get; set; }

        public string? MetricsJson { get; set; }

        public string? HyperparametersJson { get; set; }

        public string? TrainerName { get; set; }

        public int? RandomSeed { get; set; }

        public string? MlNetVersion { get; set; }

        /// <summary>Git-Commit des Trainingscodes.</summary>
        public string? CodeVersion { get; set; }

        public int FeatureSchemaVersion { get; set; }

        public string? ErrorMessage { get; set; }

        /// <summary>Das erzeugte Modell (0..1).</summary>
        public DartBotModelEntity? ResultingModel { get; set; }
    }
}
