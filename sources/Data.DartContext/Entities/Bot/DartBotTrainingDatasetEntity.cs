using Shared.Enums.Bot;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Metadaten eines Trainingsdatensatzes. Die Daten selbst liegen als Datei (CSV/Parquet) außerhalb von MySQL.
    /// </summary>
    public class DartBotTrainingDatasetEntity : AEntityBase
    {
        /// <summary>Relativer Pfad bzw. Speicher-Key, unveränderlich.</summary>
        public string StorageKey { get; set; } = null!;

        public DartBotFileFormatEnum Format { get; set; }

        public DartBotDatasetSourceTypeEnum SourceType { get; set; }

        public long RowCount { get; set; }

        public long SizeBytes { get; set; }

        public string Sha256 { get; set; } = null!;

        public int FeatureSchemaVersion { get; set; }

        // Herkunft bei Simulationen
        public int? GeneratorTargetModelId { get; set; }

        public DartBotModelEntity? GeneratorTargetModel { get; set; }

        public int? GeneratorThrowModelId { get; set; }

        public DartBotModelEntity? GeneratorThrowModel { get; set; }

        public int? RandomSeed { get; set; }

        public string? GeneratorParametersJson { get; set; }

        // Herkunft bei produktiven Spielen
        public int? FromGameId { get; set; }

        public int? ToGameId { get; set; }

        public ICollection<DartBotModelTrainingEntity> Trainings { get; set; } = [];
    }
}
