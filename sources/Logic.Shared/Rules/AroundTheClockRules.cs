namespace Logic.Shared.Rules
{
    /// <summary>
    /// Ziele 1–20, dann 21 = 25 (Single Bull, ein Treffer ins Bull zählt auch), 22 = Bull; 23 = fertig.
    /// Einzige Quelle der Regeln: Around-the-Clock-Training und Statistik nutzen diese Klasse.
    /// </summary>
    public class AroundTheClockRules : DartRulesBase
    {
        public const int FirstTarget = 1;
        public const int FinishedTarget = 23;

        public bool IsTargetHit(int target, int segment, int multiplier) => target switch
        {
            >= 1 and <= 20 => segment == target,
            21 => segment == 25,
            22 => segment == 25 && multiplier == 2,
            _ => false
        };

        public int NextTarget(int target) => target is >= 1 and < 22 ? target + 1 : FinishedTarget;
    }
}
