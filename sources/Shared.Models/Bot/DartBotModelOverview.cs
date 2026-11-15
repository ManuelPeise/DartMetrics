using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    /// <summary>Admin-Seite "ML-Modelle": Einstellungen der API und alle Zielwahl-Modelle.</summary>
    public class DartBotModelOverview
    {
        /// <summary>DartBot:Ml:Enabled – aus = Bots spielen immer regelbasiert.</summary>
        public bool MlEnabled { get; set; }

        /// <summary>DartBot:Ml:Margin – Schwelle der Hybrid-Zielwahl.</summary>
        public float Margin { get; set; }

        /// <summary>FeatureSchemaVersion, die die API erwartet.</summary>
        public int ExpectedFeatureSchemaVersion { get; set; }

        public List<DartBotModelListItem> Models { get; set; } = [];
    }

    public class DartBotModelListItem
    {
        public int Id { get; set; }
        public int VersionNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TrainingId { get; set; }
        public int DatasetId { get; set; }
        public long DatasetRowCount { get; set; }
        public long TrainingDataCount { get; set; }
        public int FeatureSchemaVersion { get; set; }
        public string MlNetVersion { get; set; } = "";
        public long SizeBytes { get; set; }

        public bool IsActive { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public string? ActivatedBy { get; set; }

        /// <summary>Prüfung der Datei auf dem Server (vorhanden, SHA-256, Schema).</summary>
        public DartBotModelFileStatusEnum FileStatus { get; set; }

        /// <summary>Aus dem Training (Testanteil): Anteil Situationen, in denen das Modell den besten Kandidaten wählt.</summary>
        public double? TopChoiceHitRate { get; set; }

        /// <summary>Aus dem Training: mittlerer Mehraufwand in Darts gegenüber dem besten Kandidaten.</summary>
        public double? TopChoiceMeanRegret { get; set; }

        /// <summary>Letzter Vergleich gegen das Regelwerk (Tools.BotTrainer compare), null = noch nicht verglichen.</summary>
        public DateTime? ComparedAt { get; set; }

        public int? ComparedLegsPerLevel { get; set; }

        public List<DartBotModelComparisonLevel> Comparison { get; set; } = [];
    }

    public class DartBotModelComparisonLevel
    {
        public float Skill { get; set; }
        public float Margin { get; set; }
        public double RuleDartsPerLeg { get; set; }
        public double ModelDartsPerLeg { get; set; }

        /// <summary>Darts pro Leg Modell − Regelwerk; negativ = Modell besser.</summary>
        public double MeanDifference { get; set; }
        public double Ci95Low { get; set; }
        public double Ci95High { get; set; }
        public double OverrideShare { get; set; }
    }
}
