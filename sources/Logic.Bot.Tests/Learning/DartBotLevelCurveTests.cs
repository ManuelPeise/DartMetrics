using Logic.Bot.Learning;
using Shared.Models.Options;

namespace Logic.Bot.Tests.Learning
{
    /// <summary>Level-Kurve, die Startlevel und Level-Aufstieg gemeinsam nutzen.</summary>
    public class DartBotLevelCurveTests
    {
        private readonly DartBotOptions _options = new();

        [Fact]
        public void LevelZero_KeepsStartProfile()
        {
            var start = _options.StartProfile.ThrowPrecision;

            Assert.Equal(start, DartBotLevelCurve.SkillForLevel(_options, start, 0));
            Assert.Equal(0, DartBotLevelCurve.XpForLevel(_options, 0));
            Assert.Equal(_options.InitialRating, DartBotLevelCurve.RatingForStartLevel(_options, 0));
        }

        [Fact]
        public void MaxLevel_ReachesSkillAtMaxLevel()
        {
            var skill = DartBotLevelCurve.SkillForLevel(_options, _options.StartProfile.ThrowPrecision, _options.MaxLevel);

            Assert.Equal(_options.Learning.SkillAtMaxLevel, skill, 3);
        }

        [Fact]
        public void Skill_IncreasesWithLevel()
        {
            var start = _options.StartProfile.ThrowConsistency;

            for (var level = 1; level <= _options.MaxLevel; level++)
            {
                Assert.True(DartBotLevelCurve.SkillForLevel(_options, start, level)
                    > DartBotLevelCurve.SkillForLevel(_options, start, level - 1));
            }
        }

        [Fact]
        public void StartRating_GrowsPerLevel()
        {
            Assert.Equal(_options.InitialRating + 10 * DartBotLevelCurve.RatingPerStartLevel,
                DartBotLevelCurve.RatingForStartLevel(_options, 10));
        }
    }
}
