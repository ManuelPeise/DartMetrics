namespace Shared.Models.Bot.Ml
{
    /// <summary>
    /// Eine Trainingszeile: Situation + Profil + Kandidat (Merkmale) und das Label.
    /// Jede Änderung der Merkmale erhöht FeatureSchemaVersion; Modelle anderer Versionen werden nicht geladen.
    /// </summary>
    public sealed class TargetCandidateFeatures
    {
        public const int FeatureSchemaVersion = 1;

        // Situation
        public float Remaining { get; set; }
        public float DartsAvailable { get; set; }
        public float OpponentRemaining { get; set; }
        /// <summary>X01OutModeEnum als Zahl (1 = Double, 2 = Straight).</summary>
        public float OutMode { get; set; }
        public bool IsOpened { get; set; }

        // Profil
        public float ThrowPrecision { get; set; }
        public float ThrowConsistency { get; set; }
        public float TacticalIntelligence { get; set; }
        public float PressureResistance { get; set; }
        public float RiskTaking { get; set; }

        // Kandidat
        public int CandidateSegment { get; set; }
        public int CandidateMultiplier { get; set; }
        public float CandidateScore { get; set; }
        public float LeftoverIfHit { get; set; }
        public bool IsFinishingDart { get; set; }
        public bool LeftoverIsCheckable { get; set; }

        /// <summary>Label: mittlere Darts bis zum Checkout ab diesem Dart (kleiner = besser).</summary>
        public float ExpectedDartsToFinish { get; set; }

        /// <summary>Streuung der Rollouts (Qualitätsprüfung, kein Merkmal).</summary>
        public float DartsStdDev { get; set; }

        public int Rollouts { get; set; }
    }
}
