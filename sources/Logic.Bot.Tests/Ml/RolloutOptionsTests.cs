using Logic.Bot.Ml;

namespace Logic.Bot.Tests.Ml
{
    public class RolloutOptionsTests
    {
        [Theory]
        [InlineData(40, 100)]
        [InlineData(100, 100)]
        [InlineData(101, 50)]
        [InlineData(300, 50)]
        [InlineData(301, 25)]
        [InlineData(501, 25)]
        public void Adaptive_DependsOnRemaining(int remaining, int expected)
        {
            var options = new RolloutOptions { RolloutsPerCandidate = 50, AdaptiveRollouts = true };

            Assert.Equal(expected, options.RolloutsFor(remaining));
        }

        [Fact]
        public void Fixed_IgnoresRemaining()
        {
            var options = new RolloutOptions { RolloutsPerCandidate = 50 };

            Assert.Equal(50, options.RolloutsFor(40));
            Assert.Equal(50, options.RolloutsFor(501));
        }
    }
}
