using Logic.Bot.Models;
using Logic.Bot.Policy;
using Logic.Bot.Tactics;
using Shared.Enums.X01;
using Shared.Models.Bot;

namespace Logic.Bot.Tests.Policy
{
    public class PolicyTableTests
    {
        private static readonly BotSkillProfile Strong = new(0.95f, 0.95f, 1f, 1f, 0.5f);

        /// <summary>Trifft immer genau das Ziel.</summary>
        private static ThrowOutcomeModel Perfect() => ThrowOutcomeModel.FromDistribution(Strong, target =>
            [new ThrowOutcomeModel.Outcome(target.Score, target.Multiplier == 2, 1f)]);

        [Theory]
        [InlineData(40, 1)]   // D20
        [InlineData(100, 2)]  // T20 D20
        [InlineData(170, 3)]  // T20 T20 Bull
        [InlineData(169, 4)]  // Bogey
        [InlineData(501, 9)]  // Neun-Darter
        public void PerfectThrower_NeedsMinimumDarts(int remaining, int expected)
        {
            var table = PolicyTable.Solve(Perfect(), X01OutModeEnum.Double, opponentPressure: false);

            Assert.Equal(expected, table.ExpectedDarts(remaining), 3);
        }

        [Fact]
        public void PerfectThrower_StraightOut_Finishes180InThree()
        {
            var table = PolicyTable.Solve(Perfect(), X01OutModeEnum.Straight, opponentPressure: false, maxScore: 180);

            Assert.Equal(3, table.ExpectedDarts(180), 3);
        }

        [Fact]
        public void Bust_IsExpensive_ForPerfectThrower()
        {
            var table = PolicyTable.Solve(Perfect(), X01OutModeEnum.Double, opponentPressure: false, maxScore: 60);
            var t20 = ThrowOutcomeModel.IndexOf(DartTarget.Triple(20));
            var d20 = ThrowOutcomeModel.IndexOf(DartTarget.Double(20));

            // Rest 40: D20 checkt, T20 überwirft (Dart zählt, Aufnahme vorbei, danach wieder 40).
            Assert.Equal(1f, table.Q(40, 40, 3, d20), 3);
            Assert.Equal(2f, table.Q(40, 40, 3, t20), 3);
        }

        [Fact]
        public void SimulatedThrower_HasFinitePlausibleValues()
        {
            var model = ThrowOutcomeModel.Build(Strong, samplesPerTarget: 1000, seed: 3);
            var table = PolicyTable.Solve(model, X01OutModeEnum.Double, opponentPressure: false, maxScore: 170);

            for (var remaining = 2; remaining <= 170; remaining++)
            {
                var value = table.ExpectedDarts(remaining);
                Assert.True(value >= 1f && value < 60f, $"Rest {remaining}: {value}");
            }

            // Ein Finish auf ein Doppel ist für einen starken Werfer schneller als ein Bogey-Rest.
            Assert.True(table.ExpectedDarts(40) < table.ExpectedDarts(169));
        }

        [Fact]
        public void Selector_AimsAtDouble_OnFinish()
        {
            var cache = new PolicyTableCache(samplesPerTarget: 1000);
            var random = new DefaultRandomSource(new Random(1));
            var selector = new PolicyTableTargetSelector(cache, new RuleBasedTargetSelector(random), random, routeTolerance: 0f);

            var target = selector.SelectTarget(new BotThrowDecisionContext(32, 3, 501, Strong));

            Assert.True(target.IsDouble, $"Ziel bei 32: {target}");
        }
    }
}
