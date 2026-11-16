using Logic.Bot.Ml;
using Logic.Bot.Tests.Helpers;
using Shared.Enums.X01;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Tests.Ml
{
    public class CandidateFeatureBuilderTests
    {
        private readonly CandidateFeatureBuilder _builder = new();

        [Fact]
        public void FinishingDouble_IsMarked()
        {
            var features = _builder.Build(new BotSituation(40, 3, 501, Profiles.Perfect), DartTarget.Double(20));

            Assert.Equal(0, features.LeftoverIfHit);
            Assert.True(features.IsFinishingDart);
            Assert.False(features.LeftoverIsCheckable);
        }

        [Fact]
        public void SetupShot_LeavesCheckableRest()
        {
            var features = _builder.Build(new BotSituation(100, 3, 501, Profiles.Perfect), DartTarget.Triple(20));

            Assert.Equal(40, features.LeftoverIfHit);
            Assert.False(features.IsFinishingDart);
            Assert.True(features.LeftoverIsCheckable);
            Assert.Equal(60, features.CandidateScore);
        }

        [Fact]
        public void BogeyRest_IsNotCheckable_WithDoubleOut()
        {
            // 169 ist mit drei Darts und Double-Out nicht checkbar.
            var features = _builder.Build(new BotSituation(229, 3, 501, Profiles.Perfect), DartTarget.Triple(20));

            Assert.Equal(169, features.LeftoverIfHit);
            Assert.False(features.LeftoverIsCheckable);
        }

        [Theory]
        [InlineData(160, 100, 2)] // T20 → 100: T20 D20
        [InlineData(100, 40, 1)]  // T20 → 40: D20
        [InlineData(229, 169, 4)] // T20 → 169: Bogey
        public void LeftoverMinDarts_IsShortestCheckout(int remaining, int leftover, int expected)
        {
            var features = _builder.Build(new BotSituation(remaining, 3, 501, Profiles.Perfect), DartTarget.Triple(20));

            Assert.Equal(leftover, features.LeftoverIfHit);
            Assert.Equal(expected, features.LeftoverMinDarts);
        }

        [Fact]
        public void LeftoverFinishableThisVisit_DependsOnDartsLeft()
        {
            // 100 → T20 → 40: mit den übrigen Darts noch zu checken, wenn danach mindestens einer übrig ist.
            var withDartsLeft = _builder.Build(new BotSituation(100, 2, 501, Profiles.Perfect), DartTarget.Triple(20));
            var lastDart = _builder.Build(new BotSituation(100, 1, 501, Profiles.Perfect), DartTarget.Triple(20));

            Assert.True(withDartsLeft.LeftoverFinishableThisVisit);
            Assert.False(lastDart.LeftoverFinishableThisVisit);
        }

        [Fact]
        public void HitProbability_IsHigherForBetterThrower_AndDeterministic()
        {
            var strong = new BotSkillProfile(0.95f, 0.95f, 1f, 1f, 0.5f);
            var weak = new BotSkillProfile(0.3f, 0.3f, 1f, 1f, 0.5f);

            var strongHit = _builder.Build(new BotSituation(501, 3, 501, strong), DartTarget.Triple(20)).HitProbability;
            var weakHit = _builder.Build(new BotSituation(501, 3, 501, weak), DartTarget.Triple(20)).HitProbability;
            var again = new CandidateFeatureBuilder().Build(new BotSituation(501, 3, 501, strong), DartTarget.Triple(20)).HitProbability;

            Assert.InRange(strongHit, 0f, 1f);
            Assert.True(strongHit > weakHit);
            Assert.Equal(strongHit, again);
        }

        [Fact]
        public void BustProbability_IsHigh_WhenAimingTooBig()
        {
            // Rest 10, Ziel S20: jeder Treffer im 20er-Segment überwirft.
            var features = _builder.Build(new BotSituation(10, 3, 501, Profiles.Perfect), DartTarget.Single(20));

            Assert.True(features.BustProbability > 0.5f);
        }

        [Fact]
        public void SituationAndProfile_AreCopied()
        {
            var profile = new BotSkillProfile(0.6f, 0.5f, 0.4f, 0.3f, 0.2f);
            var features = _builder.Build(
                new BotSituation(301, 2, 120, profile, OutMode: X01OutModeEnum.Straight), DartTarget.Single(20));

            Assert.Equal(301, features.Remaining);
            Assert.Equal(2, features.DartsAvailable);
            Assert.Equal(120, features.OpponentRemaining);
            Assert.Equal((float)X01OutModeEnum.Straight, features.OutMode);
            Assert.Equal(0.6f, features.ThrowPrecision);
            Assert.Equal(0.2f, features.RiskTaking);
        }
    }
}
