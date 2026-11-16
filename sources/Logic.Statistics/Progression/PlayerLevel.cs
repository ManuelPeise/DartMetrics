namespace Logic.Statistics.Progression
{
    /// <summary>
    /// Spieler-Level: XP nur aus beendeten Spielen (Bot-Spiele und Turnier-Matches), nie Abstieg.
    /// Gleiche Kurve wie bei den Bots: Level n ab 200 · n^1,5 XP.
    /// </summary>
    public static class PlayerLevel
    {
        public const int XpPerGame = 50;
        public const int XpPerWin = 50;
        public const int XpPerLegWon = 10;
        public const int XpPerTournamentWin = 200;
        public const int MaxLevel = 100;

        public static long XpForLevel(int level) => (long)Math.Round(200 * Math.Pow(level, 1.5));

        public static int LevelFor(long xp)
        {
            var level = 0;

            while (level < MaxLevel && xp >= XpForLevel(level + 1))
            {
                level++;
            }

            return level;
        }

        public static long ExperienceFor(ProgressionFacts facts) =>
            facts.Matches.Sum(m => (long)XpPerGame + (m.IsWon ? XpPerWin : 0) + (long)m.LegsWon * XpPerLegWon)
            + facts.Matches.Where(m => m.WonTournament).Select(m => m.TournamentId).Distinct().Count() * (long)XpPerTournamentWin;
    }
}
