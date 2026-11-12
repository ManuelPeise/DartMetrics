namespace Data.DartContext.Configurations.Training
{
    /// <summary>
    /// Shared SQL (MySQL syntax) for check constraints on single dart throws.
    /// Valid throws: 1-20 single/double/triple, 25 single (outer bull) / double (bull), 0 = miss (multiplier 0).
    /// </summary>
    internal static class DartThrowConstraints
    {
        public const string ThrowNumber = "`ThrowNumber` BETWEEN 1 AND 3";

        public const string SegmentMultiplier =
            "((`Segment` BETWEEN 1 AND 20 AND `Multiplier` BETWEEN 1 AND 3) " +
            "OR (`Segment` = 25 AND `Multiplier` BETWEEN 1 AND 2) " +
            "OR (`Segment` = 0 AND `Multiplier` = 0))";

        /// <summary>The stored score must match segment x multiplier (0..60).</summary>
        public const string Score = "`Score` = `Segment` * `Multiplier`";

        /// <summary>Target field of a training: same rule as a throw, but a miss is no valid target.</summary>
        public static string Target(string segmentColumn, string multiplierColumn) =>
            $"((`{segmentColumn}` BETWEEN 1 AND 20 AND `{multiplierColumn}` BETWEEN 1 AND 3) " +
            $"OR (`{segmentColumn}` = 25 AND `{multiplierColumn}` BETWEEN 1 AND 2))";
    }
}
