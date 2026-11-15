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
