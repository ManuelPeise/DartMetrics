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

        // Herkunft
        public DartBotProfileChangeReasonEnum ChangeReason { get; set; }

        public string? ReasonText { get; set; }

        public int? PreviousVersionId { get; set; }

        public DartBotProfileVersionEntity? PreviousVersion { get; set; }

        /// <summary>Pflicht bei ChangeReason.GameLearning (Anwendungsregel).</summary>
        public int? TriggeringGameId { get; set; }

        public DartBotGameEntity? TriggeringGame { get; set; }

        public int? ModelId { get; set; }

        public DartBotModelEntity? Model { get; set; }

        public ICollection<DartBotGamePlayerEntity> GamePlayers { get; set; } = [];
    }
}
