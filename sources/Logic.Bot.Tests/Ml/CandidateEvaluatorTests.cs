using Logic.Bot.Ml;
using Logic.Bot.Tests.Helpers;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Tests.Ml
{
    public class CandidateEvaluatorTests
    {
        private static readonly RolloutOptions Few = new() { RolloutsPerCandidate = 5, Seed = 7 };

        /// <summary>Ohne Streuung trifft der Bot jedes Ziel: die Labels sind exakt bekannt.</summary>
        private static readonly CandidateEvaluator Perfect = new(_ => new FixedRandomSource());

        private static float Darts(CandidateEvaluator evaluator, BotSituation situation, DartTarget target, RolloutOptions options) =>
            evaluator.EvaluateCandidate(situation, target, options, seed: 1).ExpectedDartsToFinish;

        [Fact]
        public void NoScatter_D20OnForty_IsOneDart()
        {
            Assert.Equal(1f, Darts(Perfect, new BotSituation(40, 3, 501, Profiles.Perfect), DartTarget.Double(20), Few));
        }

        [Fact]
        public void NoScatter_S20OnForty_IsTwoDarts()
        {
            Assert.Equal(2f, Darts(Perfect, new BotSituation(40, 3, 501, Profiles.Perfect), DartTarget.Single(20), Few));
        }

        [Fact]
        public void NoScatter_T20From501_IsNineDarter()
        {
            Assert.Equal(9f, Darts(Perfect, new BotSituation(501, 3, 501, Profiles.Perfect), DartTarget.Triple(20), Few));
        }

        [Fact]
        public void Rollouts_StopAtMaxDarts()
        {
            var hopeless = new BotSkillProfile(0f, 0f, 0f, 0f, 0f);
            var options = new RolloutOptions { RolloutsPerCandidate = 20, MaxDartsPerLeg = 15, Seed = 3 };

            var result = new CandidateEvaluator().EvaluateCandidate(new BotSituation(501, 3, 501, hopeless), DartTarget.Triple(20), options, seed: 3);

            Assert.InRange(result.ExpectedDartsToFinish, 1f, 15f);
            Assert.Equal(20, result.Rollouts);
        }

        [Fact]
        public void SameSeed_GivesSameLabels()
        {
            var situation = new BotSituation(81, 3, 140, new BotSkillProfile(0.6f, 0.6f, 0.6f, 0.6f, 0.5f));
            var options = new RolloutOptions { RolloutsPerCandidate = 10, Seed = 42 };

            var first = new CandidateEvaluator().Evaluate(situation, options).Select(f => f.ExpectedDartsToFinish).ToList();
            var second = new CandidateEvaluator().Evaluate(situation, options).Select(f => f.ExpectedDartsToFinish).ToList();

            Assert.Equal(first, second);
        }

        [Fact]
        public void StrongerProfile_NeedsFewerDarts()
        {
            var options = new RolloutOptions { RolloutsPerCandidate = 100, Seed = 11 };
            var evaluator = new CandidateEvaluator();

            var strong = Darts(evaluator, new BotSituation(501, 3, 501, new BotSkillProfile(0.9f, 0.9f, 1f, 0.9f, 0.5f)), DartTarget.Triple(20), options);
            var weak = Darts(evaluator, new BotSituation(501, 3, 501, new BotSkillProfile(0.3f, 0.5f, 1f, 0.5f, 0.5f)), DartTarget.Triple(20), options);

            Assert.True(strong < weak, $"strong={strong}, weak={weak}");
        }

        [Fact]
        public void Evaluate_RelativeDarts_BestCandidateIsZero()
        {
            var options = new RolloutOptions { RolloutsPerCandidate = 10, Seed = 5 };
            var rows = new CandidateEvaluator().Evaluate(new BotSituation(60, 3, 501, new BotSkillProfile(0.8f, 0.8f, 1f, 0.8f, 0.5f)), options);

            Assert.Equal(0f, rows.Min(r => r.RelativeDarts));
            Assert.All(rows, r => Assert.True(r.RelativeDarts >= 0));
        }

        [Fact]
        public void Evaluate_ReturnsOneRowPerCandidate()
        {
            var situation = new BotSituation(40, 3, 501, Profiles.Perfect);

            var rows = Perfect.Evaluate(situation, Few);

            Assert.Equal(new CandidateGenerator().Generate(situation).Count, rows.Count);
            Assert.All(rows, r => Assert.Equal(Few.RolloutsPerCandidate, r.Rollouts));
        }
    }
}
