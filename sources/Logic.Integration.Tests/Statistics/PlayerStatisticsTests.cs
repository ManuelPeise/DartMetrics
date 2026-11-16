using Logic.Integration.Tests.Infrastructure;
using Shared.Enums.Bot;
using Shared.Enums.Statistics;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Bot;
using Shared.Interfaces.Statistics;
using Shared.Interfaces.Training;
using Shared.Models.Bot;
using Shared.Models.Statistics;
using Shared.Models.Training;

namespace Logic.Integration.Tests.Statistics
{
    /// <summary>
    /// Statistik gegen echte Daten: Kategorien (Training/Official/Overall), Zeiträume, Vorzeitraum,
    /// Average aus Punkten und Darts, nur beendete Trainings und Spiele.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class PlayerStatisticsTests : IAsyncLifetime
    {
        private const int MaxVisits = 400;

        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public PlayerStatisticsTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task OnlyFinishedTrainingsCount_AndAverageIsPointsPerThreeDarts()
        {
            await _host.SignInNewPlayerAsync();
            var finished = await StartX01WithVisitAsync();
            await _host.RunAsync<IX01TrainingGame>(s => s.FinishTrainingAsync(finished));
            await StartX01WithVisitAsync(); // bleibt offen -> zählt nicht

            var statistics = await GetAsync(StatisticsCategoryEnum.Training);

            Assert.True(statistics.HasData);
            Assert.Equal(3, statistics.TotalDarts);
            Assert.Equal(100m, statistics.Kpis.ThreeDartAverage.Current); // T20 S20 S20 = 100 mit 3 Darts
            Assert.Equal(100, statistics.Scoring.HighestScore);
            Assert.Equal(1, statistics.Scoring.Count100Plus);
            Assert.Equal(0, statistics.LegsAndMatches.LegsPlayed); // Leg nicht beendet
            Assert.Equal(0, statistics.LegsAndMatches.MatchesPlayed); // X01-Training allein ist kein Match
            Assert.Null(statistics.Kpis.WinRate.Current);
        }

        [Fact]
        public async Task Categories_AreSeparated_AndOverallCombinesWithoutDoubleCounting()
        {
            var botId = await CreateBotAsync();
            await _host.SignInNewPlayerAsync();

            var x01 = await StartX01WithVisitAsync();
            await _host.RunAsync<IX01TrainingGame>(s => s.FinishTrainingAsync(x01));
            await PlayLostBotGameAsync(botId);

            var training = await GetAsync(StatisticsCategoryEnum.Training);
            var official = await GetAsync(StatisticsCategoryEnum.Official);
            var overall = await GetAsync(StatisticsCategoryEnum.Overall);

            Assert.False(official.HasData);
            Assert.Equal(1, training.LegsAndMatches.MatchesPlayed);
            Assert.Equal(0, training.LegsAndMatches.MatchesWon);
            Assert.Equal(0m, training.Kpis.WinRate.Current);
            Assert.Equal(3, training.LegsAndMatches.LegsPlayed); // Best of 5, der Bot gewinnt 3 Legs
            Assert.Equal(0, training.LegsAndMatches.LegsWon);

            Assert.Equal(training.TotalDarts, overall.TotalDarts);
            Assert.Equal(training.Kpis.ThreeDartAverage.Current, overall.Kpis.ThreeDartAverage.Current);
            Assert.Equal(training.LegsAndMatches.LegsPlayed, overall.LegsAndMatches.LegsPlayed);
        }

        [Fact]
        public async Task Period_FiltersByFinishDate_AndHasAPreviousPeriodOfEqualLength()
        {
            await _host.SignInNewPlayerAsync();
            var id = await StartX01WithVisitAsync();
            await _host.RunAsync<IX01TrainingGame>(s => s.FinishTrainingAsync(id));

            var now = DateTime.UtcNow;
            var last7Days = await GetAsync(StatisticsCategoryEnum.Overall, now.AddDays(-7), null);
            var future = await GetAsync(StatisticsCategoryEnum.Overall, now.AddDays(1), now.AddDays(2));
            var allTime = await GetAsync(StatisticsCategoryEnum.Overall);

            Assert.True(last7Days.HasData);
            Assert.NotNull(last7Days.PreviousFrom);
            Assert.Equal(last7Days.From, last7Days.PreviousTo);
            Assert.Null(last7Days.Kpis.ThreeDartAverage.Previous); // im Vorzeitraum nichts gespielt
            Assert.False(future.HasData);
            Assert.Null(allTime.PreviousFrom);

            await Assert.ThrowsAsync<ArgumentException>(() => GetAsync(StatisticsCategoryEnum.Overall, now, now.AddDays(-1)));
        }

        [Fact]
        public async Task NoData_ReturnsEmpty()
        {
            await _host.SignInNewPlayerAsync();

            var statistics = await GetAsync(StatisticsCategoryEnum.Overall);

            Assert.False(statistics.HasData);
            Assert.Equal(0, statistics.TotalDarts);
            Assert.Null(statistics.Kpis.ThreeDartAverage.Current);
        }

        // ---------- Hilfen ----------

        private Task<PlayerStatistics> GetAsync(StatisticsCategoryEnum category, DateTime? from = null, DateTime? to = null) =>
            _host.RunAsync<IPlayerStatisticsService, PlayerStatistics>(s => s.GetStatisticsAsync(category, from, to));

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

        private async Task<int> CreateBotAsync()
        {
            var adminId = await _host.CreateUserAsync("Admin", "User");
            _host.Claims.SignIn(adminId, UserRoleEnum.Admin);

            return await _host.RunAsync<IDartBotAdministration, int>(a => a.CreateBotAsync(new CreateDartBotModel
            {
                Name = $"Bot {Guid.NewGuid():N}"[..20],
                Alias = Guid.NewGuid().ToString("N")[..6]
            }));
        }

        /// <summary>Spieler wirft nur Fehlwürfe, der Bot (ohne Streuung) gewinnt Best of 5.</summary>
        private async Task PlayLostBotGameAsync(int botId)
        {
            var state = await _host.RunAsync<IDartBotGameService, DartBotGameState>(s => s.StartGameAsync(new StartDartBotGameModel
            {
                DartBotId = botId,
                StartingScore = 501,
                BestOfLegs = 5,
                UserStarts = true,
                GameMode = DartBotGameModeEnum.Match
            }));

            for (var visit = 0; visit < MaxVisits && state.Status == DartBotGameStatusEnum.Running; visit++)
            {
                var gameId = state.GameId;
                state = await _host.RunAsync<IDartBotGameService, DartBotGameState>(s => s.AddUserVisitAsync(new DartBotUserVisitModel
                {
                    GameId = gameId,
                    Throws = [new() { Segment = 0, Multiplier = 0 }, new() { Segment = 0, Multiplier = 0 }, new() { Segment = 0, Multiplier = 0 }]
                }));
            }

            Assert.Equal(DartBotGameStatusEnum.Finished, state.Status);
        }
    }
}
