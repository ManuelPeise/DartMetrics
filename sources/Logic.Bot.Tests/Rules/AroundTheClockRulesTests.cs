using Logic.Shared.Rules;

namespace Logic.Bot.Tests.Rules
{
    /// <summary>Around the Clock: 1–20 (jedes Feld der Zahl), 21 = 25 (auch Bull), 22 = Bull, 23 = fertig.</summary>
    public class AroundTheClockRulesTests
    {
        private static readonly AroundTheClockRules Rules = new();

        [Theory]
        [InlineData(7, 7, 1, true)]
        [InlineData(7, 7, 3, true)]
        [InlineData(7, 8, 1, false)]
        [InlineData(21, 25, 1, true)]
        [InlineData(21, 25, 2, true)]   // Bull zählt auch für das Ziel „25“
        [InlineData(21, 20, 1, false)]
        [InlineData(22, 25, 2, true)]
        [InlineData(22, 25, 1, false)]  // fürs Bull nur das Bull
        [InlineData(23, 25, 2, false)]  // fertig: nichts zählt mehr
        [InlineData(1, 0, 0, false)]
        public void IsTargetHit(int target, int segment, int multiplier, bool expected) =>
            Assert.Equal(expected, Rules.IsTargetHit(target, segment, multiplier));

        [Theory]
        [InlineData(1, 2)]
        [InlineData(20, 21)]
        [InlineData(21, 22)]
        [InlineData(22, 23)]
        [InlineData(23, 23)]
        public void NextTarget(int target, int expected) => Assert.Equal(expected, Rules.NextTarget(target));
    }
}
