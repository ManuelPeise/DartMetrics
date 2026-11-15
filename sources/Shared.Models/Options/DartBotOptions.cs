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

        /// <summary>XP, Level-Aufstieg und Elo nach einem Spiel gegen einen Menschen.</summary>
        public DartBotLearningOptions Learning { get; set; } = new();

        /// <summary>Zielwahl mit Modell (Abschnitt "DartBot:Ml").</summary>
        public DartBotMlOptions Ml { get; set; } = new();
    }

    public class DartBotMlOptions
    {
        /// <summary>Aus: Bots spielen immer regelbasiert, auch wenn ein Modell aktiv ist.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Ordner der ML-Daten; StorageKeys der Modelle sind relativ dazu (Container: /data/ml).</summary>
        [Required]
        public string Path { get; set; } = "ml_data";

        /// <summary>So viele Darts muss das Modell besser sein, damit es das Regel-Ziel ersetzt (Vergleich: 0,3 am besten).</summary>
        [Range(0.0, 10.0)]
        public float Margin { get; set; } = 0.3f;
    }

    public class DartBotLearningOptions
    {
        /// <summary>XP für jedes abgeschlossene Spiel.</summary>
        [Range(0, 100000)]
        public int XpPerGame { get; set; } = 50;

        /// <summary>Zusätzliche XP bei Sieg.</summary>
        [Range(0, 100000)]
        public int XpPerWin { get; set; } = 50;

        /// <summary>Zusätzliche XP je gewonnenem Leg.</summary>
        [Range(0, 100000)]
        public int XpPerLegWon { get; set; } = 10;

        /// <summary>Benötigte Gesamt-XP für Level n = LevelXpBase * n ^ LevelXpExponent.</summary>
        [Range(1, 1000000)]
        public int LevelXpBase { get; set; } = 200;

        [Range(1.0, 3.0)]
        public double LevelXpExponent { get; set; } = 1.5;

        /// <summary>Fähigkeitswert (Precision, Consistency, Taktik, Druck) auf dem höchsten Level.</summary>
        [Range(0.0, 1.0)]
        public float SkillAtMaxLevel { get; set; } = 0.95f;

        /// <summary>Elo-K-Faktor.</summary>
        [Range(1, 100)]
        public int EloK { get; set; } = 32;

        /// <summary>
        /// Elo eines menschlichen Gegners, falls der Benutzer nicht mehr geladen werden kann (Fallback).
        /// Regulär wird mit UserEntity.Rating gerechnet.
        /// </summary>
        [Range(0, 5000)]
        public int HumanOpponentRating { get; set; } = 1000;
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
