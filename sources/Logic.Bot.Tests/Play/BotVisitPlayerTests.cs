using Logic.Bot.Play;
using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Logic.Bot.Tests.Helpers;
using Shared.Enums.X01;
using Shared.Models.Bot;

namespace Logic.Bot.Tests.Play
{
    /// <summary>Ohne Streuung trifft der Bot immer, daher sind die Ergebnisse deterministisch.</summary>
    public class BotVisitPlayerTests
    {
        private static BotVisitPlayer CreatePerfectPlayer()
        {
            var random = new FixedRandomSource();
            return new BotVisitPlayer(new RuleBasedTargetSelector(random), new ThrowSimulator(random));
        }

        [Fact]
        public void FarFromFinish_Scores180()
        {
            var result = CreatePerfectPlayer().PlayVisit(new BotVisitContext(501, 501, Profiles.Perfect));

            Assert.Equal(3, result.DartsUsed);
            Assert.Equal(321, result.ScoreAfter);
            Assert.All(result.Throws, t => Assert.Equal(60, t.Score));
            Assert.False(result.IsBust);
        }

        [Theory]
        [InlineData(40)]
        [InlineData(100)]
        [InlineData(170)]
        public void CheckoutRange_ChecksOut(int score)
        {
            var result = CreatePerfectPlayer().PlayVisit(new BotVisitContext(score, 501, Profiles.Perfect));

            Assert.True(result.IsCheckout);
            Assert.Equal(0, result.ScoreAfter);
            Assert.Equal(2, result.Throws[^1].Multiplier);
        }

        [Fact]
        public void DoubleIn_NotOpened_OpensWithDouble()
        {
            var result = CreatePerfectPlayer().PlayVisit(
                new BotVisitContext(501, 501, Profiles.Perfect, X01InModeEnum.Double, X01OutModeEnum.Double, IsOpened: false));

            Assert.Equal(2, result.Throws[0].Multiplier);
            Assert.True(result.ScoreAfter < 501);
            Assert.Equal(501 - result.Throws.Sum(t => t.Score), result.ScoreAfter);
        }

        [Fact]
        public void ScoreBelowTwo_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                CreatePerfectPlayer().PlayVisit(new BotVisitContext(1, 501, Profiles.Perfect)));
        }
    }
}
