namespace Shared.Models.Bot
{
    /// <summary>
    /// Fähigkeiten eines Bots. Die fünf Grundwerte (0–1) gibt es seit V1; die Wurf-Eigenschaften V2 sind optional
    /// (Standard = Verhalten wie V1) und beschreiben die Streuung direkt in mm.
    /// </summary>
    public sealed record BotSkillProfile(
       float ThrowPrecision,
       float ThrowConsistency,
       float TacticalIntelligence,
       float PressureResistance,
       float RiskTaking)
    {
        /// <summary>V2: Streuung (Standardabweichung waagerecht, mm). Null = aus ThrowPrecision abgeleitet (V1).</summary>
        public float? SigmaMm { get; init; }

        /// <summary>V2: senkrechte Streuung relativ zur waagerechten (Menschen streuen nach oben/unten stärker), 1 = rund.</summary>
        public float VerticalRatio { get; init; } = 1f;

        /// <summary>V2: systematische Abweichung (mm, x nach rechts, y nach oben), z. B. „zieht nach links“.</summary>
        public float BiasXMm { get; init; }

        public float BiasYMm { get; init; }

        /// <summary>V2: Anteil richtig schlechter Darts (dreifache Streuung), auch Profis haben welche.</summary>
        public float BadDartRate { get; init; }

        /// <summary>V2: Lieblingsdoppel (weniger Streuung, Wege dorthin bevorzugt) und Angstdoppel (mehr Streuung).</summary>
        public int? FavoriteDouble { get; init; }

        public int? WeakDouble { get; init; }

        /// <summary>Faktor auf die Streuung beim Zielen auf dieses Ziel (Lieblings-/Angstdoppel).</summary>
        public float FieldFactor(DartTarget target) =>
            target.Multiplier != 2 ? 1f
            : target.Segment == FavoriteDouble ? FavoriteDoubleFactor
            : target.Segment == WeakDouble ? WeakDoubleFactor
            : 1f;

        public const float FavoriteDoubleFactor = 0.85f;
        public const float WeakDoubleFactor = 1.15f;
    }
}
