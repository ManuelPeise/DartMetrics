using Logic.Bot.Models;
using Logic.Bot.Simulation;
using Logic.Bot.Tests.Helpers;
using Shared.Models.Bot;

namespace Logic.Bot.Tests.Simulation
{
    public class ThrowSimulatorTests
    {
        [Fact]
        public void BaseSigma_HitsBounds()
        {
            Assert.Equal(ThrowSimulator.SigmaAtBest, ThrowSimulator.BaseSigma(1f), 6);
            Assert.Equal(ThrowSimulator.SigmaAtWorst, ThrowSimulator.BaseSigma(0f), 6);
        }

        [Fact]
        public void BaseSigma_DecreasesWithPrecision()
        {
            var previous = double.MaxValue;

            for (var precision = 0f; precision <= 1f; precision += 0.1f)
            {
                var sigma = ThrowSimulator.BaseSigma(precision);
                Assert.True(sigma < previous);
                previous = sigma;
            }
        }

        [Fact]
        public void BaseSigma_ClampsOutOfRange()
        {
            Assert.Equal(ThrowSimulator.BaseSigma(1f), ThrowSimulator.BaseSigma(2f));
            Assert.Equal(ThrowSimulator.BaseSigma(0f), ThrowSimulator.BaseSigma(-1f));
        }

        [Fact]
        public void Throw_WithoutScatter_LandsOnAimPoint()
        {
            var simulator = new ThrowSimulator(new FixedRandomSource());
            var aim = new BoardPoint(10, 20);

            var hit = simulator.Throw(aim, Profiles.Perfect, 1.0, isPressureSituation: true);

            Assert.Equal(aim.X, hit.X, 6);
            Assert.Equal(aim.Y, hit.Y, 6);
        }

        [Fact]
        public void Throw_BetterPrecision_HitsTripleMoreOften()
        {
            var good = HitRate(new BotSkillProfile(0.9f, 1f, 1f, 1f, 0.5f));
            var weak = HitRate(new BotSkillProfile(0.2f, 1f, 1f, 1f, 0.5f));

            Assert.True(good > weak, $"good={good}, weak={weak}");
        }

        private static double HitRate(BotSkillProfile profile)
        {
            var simulator = new ThrowSimulator(new DefaultRandomSource(new Random(42)));
            var target = DartTarget.Triple(20);
            var aim = DartBoardGeometry.GetAimPoint(target);
            var hits = 0;
            const int throws = 5000;

            for (var i = 0; i < throws; i++)
            {
                var form = simulator.DrawVisitForm(profile);

                if (DartBoardGeometry.Resolve(simulator.Throw(aim, profile, form, false)) == target)
                {
                    hits++;
                }
            }

            return hits / (double)throws;
        }
    }
}
