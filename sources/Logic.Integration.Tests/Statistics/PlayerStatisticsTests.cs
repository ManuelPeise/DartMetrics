using Logic.Integration.Tests.Infrastructure;
using Shared.Enums.Statistics;
using Shared.Interfaces.Statistics;
using Shared.Interfaces.Training;
using Shared.Models.Statistics;
using Shared.Models.Training;

namespace Logic.Integration.Tests.Statistics
{
    [Collection(MySqlCollection.Name)]
    public class PlayerStatisticsTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public PlayerStatisticsTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _host = await TestHost.CreateAsync(_fixture);
            await _host.SignInNewPlayerAsync();
        }

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task OnlyFinishedTrainingsCount_AndAverageMatchesResult()
        {
            var finished = await StartX01WithVisitAsync();
            await _host.RunAsync<IX01TrainingGame>(s => s.FinishTrainingAsync(finished));
            await StartX01WithVisitAsync(); // bleibt offen -> zählt nicht

            var statistics = await _host.RunAsync<IPlayerStatisticsService, PlayerStatistics>(s =>
                s.GetStatisticsAsync(StatisticsPeriodEnum.Last30Days));
            var result = await _host.RunAsync<IX01TrainingGame, X01TrainingResult>(s => s.GetResultAsync(finished));

            Assert.True(statistics.HasData);
            Assert.False(statistics.IsFallbackToAll);
            Assert.Equal(1, statistics.X01.Sessions);
            Assert.Equal(result.AverageScorePerVisit, statistics.X01.AveragePerVisit);
            Assert.Equal(100, statistics.X01.BestVisit);
        }

        [Fact]
        public async Task NoData_ReturnsEmpty()
        {
            var statistics = await _host.RunAsync<IPlayerStatisticsService, PlayerStatistics>(s =>
                s.GetStatisticsAsync(StatisticsPeriodEnum.Last30Days));

            Assert.False(statistics.HasData);
            Assert.False(statistics.IsFallbackToAll);
        }

        /// <summary>Eine Aufnahme T20, S20, S20 = 100 Punkte.</summary>
        private async Task<int> StartX01WithVisitAsync()
        {
            var id = await _host.RunAsync<IX01TrainingGame, int>(s => s.StartTrainingAsync(new StartX01TrainingModel { StartingScore = 501 }));

            var darts = new[] { (20, 3), (20, 1), (20, 1) };

            for (var i = 0; i < darts.Length; i++)
            {
                var (segment, multiplier) = darts[i];
                var throwNumber = i + 1;
                await _host.RunAsync<IX01TrainingGame, X01TrainingThrowResult>(s => s.AddThrowAsync(
                    new X01TrainingThrow { TrainingId = id, VisitNumber = 1, ThrowNumber = throwNumber, Segment = segment, Multiplier = multiplier }));
            }

            return id;
        }
    }
}
