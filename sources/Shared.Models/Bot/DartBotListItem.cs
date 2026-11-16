using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    /// <summary>Übersicht eines Bots inklusive der aktuellen Profilversion.</summary>
    public class DartBotListItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Alias { get; set; } = null!;

        public bool IsActive { get; set; }

        public int DevelopmentLevel { get; set; }

        public long ExperiencePoints { get; set; }

        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; }

        public int ProfileVersionNumber { get; set; }

        public float ThrowPrecision { get; set; }

        public float ThrowConsistency { get; set; }

        public float TacticalIntelligence { get; set; }

        public float PressureResistance { get; set; }

        public float RiskTaking { get; set; }

        public DartBotPlayingStyleEnum? PlayingStyle { get; set; }

        /// <summary>V2: Basis-Ø, Potenzial und Stufe (null bei V1-Profilen).</summary>
        public float? BaseAverage { get; set; }

        public float? PotentialAverage { get; set; }

        public string? StageName { get; set; }

        public float Nervousness { get; set; }
    }
}
