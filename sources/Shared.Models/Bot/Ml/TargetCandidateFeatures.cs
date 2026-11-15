namespace Shared.Models.Bot.Ml
{
    /// <summary>
    /// Eine Trainingszeile: Situation + Profil + Kandidat (Merkmale) und das Label.
    /// Jede Änderung der Merkmale erhöht FeatureSchemaVersion; Modelle anderer Versionen werden nicht geladen.
    /// </summary>
    public sealed class TargetCandidateFeatures
    {
        /// <summary>
        /// 2: SituationId und relatives Label (RelativeDarts) ergänzt; Rollouts aller Kandidaten einer Situation
        /// nutzen denselben Zufallsstrom.
        /// </summary>
        public const int FeatureSchemaVersion = 2;

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

        /// <summary>Gruppierung: alle Kandidaten einer Situation tragen dieselbe Nummer (kein Merkmal).</summary>
        public int SituationId { get; set; }

        /// <summary>
        /// Label (Schema 2): Darts mehr als der beste Kandidat derselben Situation (0 = bester).
        /// Für die Zielwahl zählt die Rangfolge innerhalb der Situation, nicht der absolute Wert.
        /// </summary>
        public float RelativeDarts { get; set; }
    }
}
