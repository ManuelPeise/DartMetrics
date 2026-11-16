using Logic.Bot.Administration;
using Logic.Bot.Simulation;
using Shared.Enums.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Tests.Simulation
{
    public class AverageCalibrationTests
    {
        [Theory]
        [InlineData(99.8f, 4.78f)]
        [InlineData(55.5f, 11.34f)]
        [InlineData(24.7f, 23.29f)]
        public void MeasuredPoints_AreHitExactly(float average, float sigma)
        {
            Assert.Equal(sigma, AverageCalibration.SigmaForAverage(average), 2);
            Assert.Equal(average, AverageCalibration.AverageForSigma(sigma), 1);
        }

        [Fact]
        public void HigherAverage_MeansLessScatter()
        {
            var previous = float.MaxValue;

            for (var average = 15f; average <= 115f; average += 5f)
            {
                var sigma = AverageCalibration.SigmaForAverage(average);
                Assert.True(sigma < previous, $"Ø {average}: {sigma}");
                previous = sigma;
            }
        }

        [Fact]
        public void RoundTrip_IsStable()
        {
            foreach (var average in new[] { 25f, 40f, 55f, 70f, 85f, 95f, 105f })
            {
                Assert.Equal(average, AverageCalibration.AverageForSigma(AverageCalibration.SigmaForAverage(average)), 0);
            }
        }

        [Theory]
        [InlineData(25f, DartBotStageEnum.Beginner)]
        [InlineData(47f, DartBotStageEnum.Hobby)]
        [InlineData(48f, DartBotStageEnum.Club)]
        [InlineData(97.5f, DartBotStageEnum.WorldClass)]
        [InlineData(110f, DartBotStageEnum.WorldChampion)]
        public void StageFor_UsesMidpoints(float average, DartBotStageEnum expected)
        {
            Assert.Equal(expected, DartBotStages.StageFor(average));
        }

        [Fact]
        public void Factory_StrongerBots_HaveFewerBadDartsAndKnowRoutes()
        {
            var random = new Random(1);
            var beginner = BotProfileFactory.Create(25f, null, random);
            var champion = BotProfileFactory.Create(105f, null, random);

            Assert.True(champion.SigmaMm < beginner.SigmaMm);
            Assert.True(champion.BadDartRate < beginner.BadDartRate);
            Assert.True(champion.TacticalIntelligence > beginner.TacticalIntelligence);
            Assert.Equal(105f, champion.BaseAverage);
        }
    }
}
