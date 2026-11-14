using Logic.Bot.Tactics;
using Logic.Bot.Tests.Helpers;
using Shared.Enums.X01;
using Shared.Models.Bot;

namespace Logic.Bot.Tests.Tactics
{
    public class RuleBasedTargetSelectorTests
    {
        private readonly RuleBasedTargetSelector _selector = new(new FixedRandomSource());

        private DartTarget Select(int remaining, int darts = 3, X01InModeEnum inMode = X01InModeEnum.Straight,
            X01OutModeEnum outMode = X01OutModeEnum.Double, bool isOpened = true) =>
            _selector.SelectTarget(new BotThrowDecisionContext(remaining, darts, 501, Profiles.Perfect, inMode, outMode, isOpened));

        [Fact]
        public void FarAway_AimsT20()
        {
            Assert.Equal(DartTarget.Triple(20), Select(501));
        }

        [Fact]
        public void DoubleIn_NotOpened_AimsDouble()
        {
            Assert.True(Select(501, inMode: X01InModeEnum.Double, isOpened: false).IsDouble);
        }

        [Theory]
        [InlineData(40, 20)]
        [InlineData(32, 16)]
        [InlineData(50, 25)]
        public void OneDartFinish_AimsDouble(int remaining, int expectedSegment)
        {
            Assert.Equal(DartTarget.Double(expectedSegment), Select(remaining, darts: 1));
        }

        [Fact]
        public void MasterOut_OneDart_CanAimTriple()
        {
            // 57 ist nur als T19 in einem Dart zu checken.
            Assert.Equal(DartTarget.Triple(19), Select(57, darts: 1, outMode: X01OutModeEnum.Master));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(2)]
        [InlineData(1)]
        public void Target_NeverBustsOnSingleDart(int darts)
        {
            for (var remaining = 2; remaining <= 170; remaining++)
            {
                var target = Select(remaining, darts);
                var rest = remaining - target.Score;

                Assert.True(rest >= 2 || (rest == 0 && target.IsDouble),
                    $"Rest {remaining}, {darts} Darts: {target} führt zu {rest}.");
            }
        }
    }
}
