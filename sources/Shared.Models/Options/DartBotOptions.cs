using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Options
{
    /// <summary>
    /// Einstellungen für die DartBots (Abschnitt "DartBot" in appsettings).
    /// Ohne Eintrag gelten die Standardwerte.
    /// </summary>
    public class DartBotOptions
    {
        /// <summary>Höchstes Entwicklungslevel (Start ist Level 0).</summary>
        [Range(1, 100)]
        public int MaxLevel { get; set; } = 20;

        /// <summary>Elo-Startwert eines neuen Bots.</summary>
        [Range(0, 5000)]
        public int InitialRating { get; set; } = 1000;

        /// <summary>Fähigkeiten eines neuen Bots auf Level 0 (etwa 30 Punkte pro Aufnahme).</summary>
        public DartBotSkillOptions StartProfile { get; set; } = new();
    }

    public class DartBotSkillOptions
    {
        [Range(0.0, 1.0)]
        public float ThrowPrecision { get; set; } = 0.15f;

        [Range(0.0, 1.0)]
        public float ThrowConsistency { get; set; } = 0.2f;

        [Range(0.0, 1.0)]
        public float TacticalIntelligence { get; set; } = 0.2f;

        [Range(0.0, 1.0)]
        public float PressureResistance { get; set; } = 0.2f;

        [Range(0.0, 1.0)]
        public float RiskTaking { get; set; } = 0.5f;
    }
}
