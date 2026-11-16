using Logic.Statistics.Progression;

namespace Logic.Statistics.Tests
{
    public class ProgressionTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(199, 0)]
        [InlineData(200, 1)]
        [InlineData(565, 1)]
        [InlineData(566, 2)]
        public void LevelFor_FollowsCurve(long xp, int expected)
        {
            Assert.Equal(expected, PlayerLevel.LevelFor(xp));
        }

        [Fact]
        public void LevelFor_IsCappedAtMax()
        {
            Assert.Equal(PlayerLevel.MaxLevel, PlayerLevel.LevelFor(long.MaxValue / 2));
        }

        [Fact]
        public void XpForLevel_IsStrictlyIncreasing()
        {
            for (var level = 1; level < PlayerLevel.MaxLevel; level++)
                Assert.True(PlayerLevel.XpForLevel(level + 1) > PlayerLevel.XpForLevel(level));
        }

        [Fact]
        public void Catalog_KeysAreUniqueAndFitColumn()
        {
            var keys = AchievementCatalog.All.Select(a => a.Key).ToList();

            Assert.Equal(keys.Count, keys.Distinct().Count());
            Assert.All(keys, k => Assert.InRange(k.Length, 1, 60));
        }

        [Fact]
        public void Catalog_Find_ReturnsDefinitionOrNull()
        {
            Assert.NotNull(AchievementCatalog.Find("first-180"));
            Assert.Null(AchievementCatalog.Find("does-not-exist"));
        }
    }
}
