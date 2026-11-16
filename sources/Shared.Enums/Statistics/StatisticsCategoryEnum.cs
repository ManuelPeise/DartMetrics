namespace Shared.Enums.Statistics
{
    /// <summary>Kategorie der Spieler-Statistik.</summary>
    public enum StatisticsCategoryEnum
    {
        /// <summary>Training und Official zusammen (ohne Doppelzählung).</summary>
        Overall = 1,

        /// <summary>X01-Training allein und alle Spiele gegen Bots (Modus Training und Match) sowie die Übungen.</summary>
        Training = 2,

        /// <summary>Turnier-Matches (Liga, K.-o.; auch gegen Bots).</summary>
        Official = 3
    }
}
