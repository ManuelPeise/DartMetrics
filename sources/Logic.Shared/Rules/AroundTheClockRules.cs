namespace Logic.Shared.Rules
{
    /// <summary>
    /// Ziele 1–20, dann 21 = 25 (Single Bull), 22 = Bull; 23 = fertig.
    /// Hinweis: Logic.Training.AroundTheClockTraining hat die gleiche Logik noch privat (unverändert gelassen).
    /// </summary>
    public class AroundTheClockRules : DartRulesBase
    {
        public const int FirstTarget = 1;
        public const int FinishedTarget = 23;

        public bool IsTargetHit(int target, int segment, int multiplier) => target switch
        {
            >= 1 and <= 20 => segment == target,
            21 => segment == 25 && multiplier == 1,
            22 => segment == 25 && multiplier == 2,
            _ => false
        };

        public int NextTarget(int target) => target is >= 1 and < 22 ? target + 1 : FinishedTarget;
    }
}
