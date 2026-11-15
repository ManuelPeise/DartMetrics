using Logic.Bot.Ml;
using Logic.Bot.Tests.Helpers;
using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Tests.Ml
{
    public class CandidateGeneratorTests
    {
        private readonly CandidateGenerator _generator = new();
        private readonly X01Rules _rules = new();

        [Theory]
        [InlineData(X01OutModeEnum.Double)]
        [InlineData(X01OutModeEnum.Straight)]
        public void NoCandidate_IsASureBust(X01OutModeEnum outMode)
        {
            for (var remaining = X01Rules.MinRemaining(outMode); remaining <= 501; remaining++)
            {
                var situation = new BotSituation(remaining, 3, 501, Profiles.Perfect, OutMode: outMode);

                foreach (var target in _generator.Generate(situation))
                {
                    var leftover = remaining - target.Score;

                    Assert.False(_rules.IsBust(remaining, target.Score, target.Segment, target.Multiplier, outMode),
                        $"Rest {remaining}, {outMode}: {target} bustet sicher.");
                    Assert.True(leftover >= 0, $"Rest {remaining}: {target}");
                }
            }
        }

        [Fact]
        public void FarFromFinish_OffersAllTargets()
        {
            Assert.Equal(62, _generator.Generate(new BotSituation(501, 3, 501, Profiles.Perfect)).Count);
        }

        [Fact]
        public void StraightOut_RestOne_OnlySingleOne()
        {
            var candidates = _generator.Generate(new BotSituation(1, 1, 501, Profiles.Perfect, OutMode: X01OutModeEnum.Straight));

            Assert.Equal(DartTarget.Single(1), Assert.Single(candidates));
        }

        [Fact]
        public void DoubleOut_Rest40_ExcludesSureBusts()
        {
            var candidates = _generator.Generate(new BotSituation(40, 3, 501, Profiles.Perfect));

            Assert.Contains(DartTarget.Double(20), candidates);
            Assert.DoesNotContain(candidates, c => 40 - c.Score == 1);   // Rest 1 = Bust bei Double-Out
            Assert.DoesNotContain(DartTarget.Triple(20), candidates); // 60 > 40
            Assert.DoesNotContain(DartTarget.Double(25), candidates); // 50 > 40
        }

        [Fact]
        public void DoubleIn_NotOpened_OnlyDoubles()
        {
            var situation = new BotSituation(501, 3, 501, Profiles.Perfect, InMode: X01InModeEnum.Double, IsOpened: false);

            var candidates = _generator.Generate(situation);

            Assert.Equal(21, candidates.Count); // D1–D20 + Bull
            Assert.All(candidates, c => Assert.True(c.IsDouble));
        }
    }
}
