using Shared.Enums.Bot;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Unveränderliche Version der Fähigkeiten eines Bots (nur anhängen, nie ändern).
    /// Alle Fähigkeitswerte liegen im Bereich 0.0 bis 1.0.
    /// </summary>
    public class DartBotProfileVersionEntity : AEntityBase
    {
        public int DartBotId { get; set; }

        public DartBotEntity DartBot { get; set; } = null!;

        public int VersionNumber { get; set; }

        // Wurf
        public float ThrowPrecision { get; set; }

        public float ThrowConsistency { get; set; }

        // Taktik
        public float TacticalIntelligence { get; set; }

        public float PressureResistance { get; set; }

        /// <summary>Präferenz, keine Fähigkeit.</summary>
        public float RiskTaking { get; set; }

        public DartBotPlayingStyleEnum? PlayingStyle { get; set; }

        // ---- Wurf-Steckbrief V2 (null = Verhalten V1, aus ThrowPrecision abgeleitet) ----

        /// <summary>Basis-Durchschnitt (3 Darts) an einem normalen Tag; daraus wurde die Streuung kalibriert.</summary>
        public float? BaseAverage { get; set; }

        /// <summary>Streuung waagerecht (Standardabweichung, mm).</summary>
        public float? SigmaMm { get; set; }

        /// <summary>Senkrechte Streuung relativ zur waagerechten (Menschen streuen nach oben/unten stärker).</summary>
        public float VerticalRatio { get; set; } = 1f;

        /// <summary>Systematische Abweichung in mm (x nach rechts, y nach oben).</summary>
        public float BiasXMm { get; set; }

        public float BiasYMm { get; set; }

        /// <summary>Anteil schlechter Darts (dreifache Streuung).</summary>
        public float BadDartRate { get; set; }

        /// <summary>Wie stark eigene Fehler nachwirken (0 = cool, 1 = sehr nervös).</summary>
        public float Nervousness { get; set; }

        // Herkunft
        public DartBotProfileChangeReasonEnum ChangeReason { get; set; }

        public string? ReasonText { get; set; }

        public int? PreviousVersionId { get; set; }

        public DartBotProfileVersionEntity? PreviousVersion { get; set; }

        /// <summary>Pflicht bei ChangeReason.GameLearning (Anwendungsregel).</summary>
        public int? TriggeringGameId { get; set; }

        public DartBotGameEntity? TriggeringGame { get; set; }

        public ICollection<DartBotGamePlayerEntity> GamePlayers { get; set; } = [];
    }
}
