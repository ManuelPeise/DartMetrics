using Logic.Bot.Learning;
using Microsoft.Extensions.Options;
using Shared.Models.Options;

namespace Logic.Bot.Tests.Learning
{
    /// <summary>Nur die reine Level-Berechnung; Datenbankpfade gehören in Integrationstests.</summary>
    public class DartBotLearningServiceLevelTests
    {
        private readonly DartBotOptions _options = new();
        private readonly DartBotLearningService _service;

        public DartBotLearningServiceLevelTests()
        {
            _service = new DartBotLearningService(null!, Options.Create(_options));
        }

        [Fact]
        public void ZeroXp_IsLevelZero()
        {
            Assert.Equal(0, _service.LevelForExperience(0));
        }

        [Fact]
        public void XpThreshold_ReachesExactlyThatLevel()
        {
            for (var level = 1; level <= _options.MaxLevel; level++)
            {
                var xp = _service.XpForLevel(level);

                Assert.Equal(level, _service.LevelForExperience(xp));
                Assert.Equal(level - 1, _service.LevelForExperience(xp - 1));
            }
        }

        [Fact]
        public void XpForLevel_IsStrictlyIncreasing()
        {
            for (var level = 1; level <= _options.MaxLevel; level++)
            {
                Assert.True(_service.XpForLevel(level) > _service.XpForLevel(level - 1));
            }
        }

        [Fact]
        public void Level_IsCappedAtMaxLevel()
        {
            Assert.Equal(_options.MaxLevel, _service.LevelForExperience(long.MaxValue / 2));
        }
    }
}
