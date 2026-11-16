using Shared.Models.Options;

namespace Logic.Bot.Learning
{
    /// <summary>
    /// Level-Kurve der Bots: XP-Schwellen und Fähigkeitswerte je Level.
    /// Wird vom Lernen (Level-Aufstieg) und beim Anlegen mit Startlevel gemeinsam genutzt.
    /// </summary>
    public static class DartBotLevelCurve
    {
        /// <summary>Benötigte Gesamt-XP für ein Level.</summary>
        public static long XpForLevel(DartBotOptions options, int level) =>
            (long)Math.Round(options.Learning.LevelXpBase * Math.Pow(level, options.Learning.LevelXpExponent));

        /// <summary>Gleichmäßig vom Startwert (Level 0) bis SkillAtMaxLevel (MaxLevel).</summary>
        public static float SkillForLevel(DartBotOptions options, float startValue, int level)
        {
            var target = Math.Max(startValue, options.Learning.SkillAtMaxLevel);
            var progress = Math.Clamp(level / (float)options.MaxLevel, 0f, 1f);

            return Math.Clamp(startValue + (target - startValue) * progress, 0f, 1f);
        }

        /// <summary>Startwertung abhängig vom Startlevel (pro Level +RatingPerStartLevel).</summary>
        public static int RatingForStartLevel(DartBotOptions options, int level) =>
            options.InitialRating + RatingPerStartLevel * level;

        public const int RatingPerStartLevel = 40;
    }
}
