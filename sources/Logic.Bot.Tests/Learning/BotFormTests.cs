using Data.DartContext.Entities.Bot;
using Logic.Bot.Learning;
using Logic.Bot.Models;

namespace Logic.Bot.Tests.Learning
{
    public class BotFormTests
    {
        [Fact]
        public void Form_StaysWithinLimits_AndHasStreaks()
        {
            var random = new DefaultRandomSource(new Random(5));
            var form = 0f;
            var forms = new List<float>();

            for (var game = 0; game < 2000; game++)
            {
                form = BotForm.NextForm(form, consistency: 0.3f, random);
                forms.Add(form);
                Assert.InRange(form, -BotForm.Limit, BotForm.Limit);
            }

            // Serien: aufeinanderfolgende Spiele hängen deutlich zusammen (Korrelation nahe Persistence).
            var mean = forms.Average();
            var covariance = forms.Zip(forms.Skip(1), (a, b) => (a - mean) * (b - mean)).Average();
            var variance = forms.Average(f => (f - mean) * (f - mean));
            Assert.InRange(covariance / variance, 0.55, 0.85);
        }

        [Fact]
        public void PerfectConsistency_HasNoSwing()
        {
            var random = new DefaultRandomSource(new Random(1));

            Assert.Equal(0f, BotForm.NextForm(0f, consistency: 1f, random));
        }

        [Fact]
        public void Win_ImprovesForm_LossWorsens()
        {
            Assert.True(BotForm.AfterResult(0f, won: true) < 0f);
            Assert.True(BotForm.AfterResult(0f, won: false) > 0f);
        }

        [Fact]
        public void Nerves_CountMissedDoublesAndBusts_UntilCheckout()
        {
            var missed = Visit(throws: [Dart(target: 16, scoreBefore: 32, segment: 8, multiplier: 1)]);
            var bust = Visit(isBust: true);
            var checkout = Visit(isCheckout: true);

            Assert.Equal(1f, BotForm.NervesFactor([], 1f));
            Assert.Equal(1f + 2 * BotForm.NervesPerEvent, BotForm.NervesFactor([missed, bust], 1f), 4);
            Assert.Equal(1f, BotForm.NervesFactor([missed, bust, checkout], 1f));
            Assert.Equal(1f, BotForm.NervesFactor([missed, bust], 0f));
        }

        private static DartBotGameVisitEntity Visit(bool isBust = false, bool isCheckout = false, List<DartBotGameThrowEntity>? throws = null) =>
            new() { IsBust = isBust, IsCheckout = isCheckout, Throws = throws ?? [] };

        private static DartBotGameThrowEntity Dart(int target, int scoreBefore, int segment, int multiplier) => new()
        {
            TargetSegment = target,
            TargetMultiplier = 2,
            ScoreBeforeThrow = scoreBefore,
            Segment = segment,
            Multiplier = multiplier
        };
    }
}
