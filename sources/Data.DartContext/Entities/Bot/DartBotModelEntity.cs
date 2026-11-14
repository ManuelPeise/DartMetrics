using Shared.Enums.Bot;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Ein gemeinsames ML.NET-Modell (für alle Bots). Die Datei wird nie überschrieben.
    /// Welches Modell aktiv ist, steht in <see cref="DartBotActiveModelEntity"/>.
    /// </summary>
    public class DartBotModelEntity : AEntityBase
    {
        public DartBotModelTypeEnum ModelType { get; set; }

        /// <summary>Fortlaufend je ModelType.</summary>
        public int VersionNumber { get; set; }

        /// <summary>Trainingslauf, aus dem das Modell stammt (eindeutig).</summary>
        public int TrainingId { get; set; }

        public DartBotModelTrainingEntity Training { get; set; } = null!;

        /// <summary>Relativer Pfad bzw. Speicher-Key.</summary>
        public string StorageKey { get; set; } = null!;

        public DartBotFileFormatEnum Format { get; set; }

        public long SizeBytes { get; set; }

        public string Sha256 { get; set; } = null!;

        public int FeatureSchemaVersion { get; set; }

        public string MlNetVersion { get; set; } = null!;
    }
}
