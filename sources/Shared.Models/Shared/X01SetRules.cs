namespace Shared.Models.Shared
{
    /// <summary>
    /// X01 wird in genau einem Satz gespielt. Ein Satz ist „Best of N Legs“; er endet, sobald ein Spieler
    /// die Mehrheit hat (First to N/2 + 1), weil der Gegner das nicht mehr aufholen kann.
    /// Gilt für Server (Prüfung) und Client (Auswahl).
    /// </summary>
    public static class X01SetRules
    {
        /// <summary>Wählbare Satzlängen für Match (und später je Turnierrunde).</summary>
        public static readonly IReadOnlyList<int> AllowedBestOfLegs = [5, 7, 9, 13];

        /// <summary>Training gegen einen Bot und X01-Training allein: immer Best of 5.</summary>
        public const int TrainingBestOfLegs = 5;

        /// <summary>Legs, die zum Satzgewinn nötig sind (Best of 5 → 3, 7 → 4, 9 → 5, 13 → 7).</summary>
        public static int LegsToWin(int bestOfLegs) => bestOfLegs / 2 + 1;
    }
}
