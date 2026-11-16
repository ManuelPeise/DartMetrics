using Data.DartContext;
using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Logic.Integration.Tests.Bot
{
    /// <summary>
    /// Ganzes Spiel (Satz Best of 5) gegen einen Bot ohne Streuung: der Spieler wirft nur Misses, der Bot checkt jedes Leg
    /// nach 3 Aufnahmen (180, 180, 141) und gewinnt 3:0. Prüft Spielablauf, Lernschritt (XP, Elo Bot + Spieler) und dass nichts doppelt gewertet wird.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class BotGameFlowTests : IAsyncLifetime
    {
        private const int MaxVisits = 30;

        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public BotGameFlowTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task FinishedGame_AppliesLearningOnce_ForBotAndPlayer()
        {
            var botId = await CreateBotAsync();
            var userId = await _host.SignInNewPlayerAsync();

            var state = await StartGameAsync(botId);
            Assert.Equal(DartBotGameStatusEnum.Running, state.Status);

            state = await PlayUntilFinishedAsync(state.GameId);

            Assert.Equal(DartBotGameStatusEnum.Finished, state.Status);
            Assert.False(state.IsUserWinner);
            Assert.True(state.BotXpGained > 0);
            Assert.True(state.BotRatingAfter > state.BotRatingBefore);
            Assert.True(state.UserRatingAfter < state.UserRatingBefore);

            // Erneutes Laden darf nichts neu werten.
            var reloaded = await _host.RunAsync<IDartBotGameService, DartBotGameState>(s => s.GetGameAsync(state.GameId));
            Assert.Equal(state.BotRatingAfter, reloaded.BotRatingAfter);

            var (botXp, botRating, userRating) = await _host.DbAsync(async db =>
            {
                var bot = await db.Set<Data.DartContext.Entities.Bot.DartBotEntity>().SingleAsync(b => b.Id == botId);
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                return (bot.ExperiencePoints, bot.Rating, user.Rating);
            });

            Assert.Equal(state.BotXpGained!.Value, botXp);
            Assert.Equal(state.BotRatingAfter!.Value, botRating);
            Assert.Equal(state.UserRatingAfter!.Value, userRating);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(11)]
        public async Task Match_WithUnsupportedSetLength_IsRejected(int bestOfLegs)
        {
            var botId = await CreateBotAsync();
            await _host.SignInNewPlayerAsync();

            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<IDartBotGameService, DartBotGameState>(s =>
                s.StartGameAsync(new StartDartBotGameModel { DartBotId = botId, BestOfLegs = bestOfLegs, GameMode = DartBotGameModeEnum.Match })));
        }

        [Fact]
        public async Task Training_IsAlwaysBestOfFive()
        {
            var botId = await CreateBotAsync();
            await _host.SignInNewPlayerAsync();

            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<IDartBotGameService, DartBotGameState>(s =>
                s.StartGameAsync(new StartDartBotGameModel { DartBotId = botId, BestOfLegs = 7, GameMode = DartBotGameModeEnum.Training })));
        }

        [Fact]
        public async Task RunningGame_CanBeResumed_AndIsGoneAfterAbort()
        {
            var botId = await CreateBotAsync();
            await _host.SignInNewPlayerAsync();

            var started = await StartGameAsync(botId);
            await SendMissesAsync(started.GameId);

            var running = await _host.RunAsync<IDartBotGameService, DartBotGameState?>(s => s.GetRunningGameAsync());
            Assert.NotNull(running);
            Assert.Equal(started.GameId, running.GameId);

            await _host.RunAsync<IDartBotGameService>(s => s.AbortGameAsync(started.GameId));

            var afterAbort = await _host.RunAsync<IDartBotGameService, DartBotGameState?>(s => s.GetRunningGameAsync());
            Assert.Null(afterAbort);
        }

        [Fact]
        public async Task AbortedGame_ChangesNoRating()
        {
            var botId = await CreateBotAsync();
            var userId = await _host.SignInNewPlayerAsync();

            var started = await StartGameAsync(botId);
            await _host.RunAsync<IDartBotGameService>(s => s.AbortGameAsync(started.GameId));

            var (botRating, userRating) = await _host.DbAsync(async db =>
                ((await db.Set<Data.DartContext.Entities.Bot.DartBotEntity>().SingleAsync(b => b.Id == botId)).Rating,
                 (await db.Users.SingleAsync(u => u.Id == userId)).Rating));

            Assert.Equal(1000, botRating);
            Assert.Equal(1000, userRating);
        }

        [Fact]
        public async Task RatingHistory_ListsFinishedGame()
        {
            var botId = await CreateBotAsync();
            await _host.SignInNewPlayerAsync();

            var state = await PlayUntilFinishedAsync((await StartGameAsync(botId)).GameId);

            var history = await _host.RunAsync<IDartBotGameService, List<DartBotRatingHistoryEntry>>(s => s.GetRatingHistoryAsync(20));

            var entry = Assert.Single(history);
            Assert.Equal(state.GameId, entry.GameId);
            Assert.False(entry.IsWin);
            Assert.Equal(state.UserRatingAfter!.Value, entry.RatingAfter);
        }

        [Fact]
        public async Task CreateBot_WithStartLevel_SetsLevelXpRatingAndProfile()
        {
            var adminId = await _host.CreateUserAsync("Admin", "User");
            _host.Claims.SignIn(adminId, UserRoleEnum.Admin);

            var botId = await _host.RunAsync<IDartBotAdministration, int>(a => a.CreateBotAsync(new CreateDartBotModel
            {
                Name = "Startlevel Bot",
                Alias = Guid.NewGuid().ToString("N")[..6],
                StartLevel = 10
            }));

            var bot = await _host.DbAsync(db => db.Set<Data.DartContext.Entities.Bot.DartBotEntity>()
                .Include(b => b.ProfileVersions)
                .SingleAsync(b => b.Id == botId));

            Assert.Equal(10, bot.DevelopmentLevel);
            Assert.True(bot.ExperiencePoints > 0);
            Assert.True(bot.Rating > 1000);

            var profile = Assert.Single(bot.ProfileVersions);
            Assert.Equal(DartBotProfileChangeReasonEnum.Initial, profile.ChangeReason);
            Assert.Equal("Startprofil Level 10", profile.ReasonText);
            Assert.True(profile.ThrowPrecision > 0.15f);
        }

        [Fact]
        public async Task CreateBot_WithInvalidStartLevel_Throws()
        {
            var adminId = await _host.CreateUserAsync("Admin", "User");
            _host.Claims.SignIn(adminId, UserRoleEnum.Admin);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _host.RunAsync<IDartBotAdministration, int>(a => a.CreateBotAsync(new CreateDartBotModel
                {
                    Name = "Zu hoch",
                    Alias = Guid.NewGuid().ToString("N")[..6],
                    StartLevel = 99
                })));
        }

        // ---------- Hilfen ----------

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

        private Task<DartBotGameState> StartGameAsync(int botId) =>
            _host.RunAsync<IDartBotGameService, DartBotGameState>(s => s.StartGameAsync(new StartDartBotGameModel
            {
                DartBotId = botId,
                StartingScore = 501,
                BestOfLegs = 5,
                UserStarts = true,
                GameMode = DartBotGameModeEnum.Match
            }));

        private Task<DartBotGameState> SendMissesAsync(int gameId) =>
            _host.RunAsync<IDartBotGameService, DartBotGameState>(s => s.AddUserVisitAsync(new DartBotUserVisitModel
            {
                GameId = gameId,
                Throws = [new() { Segment = 0, Multiplier = 0 }, new() { Segment = 0, Multiplier = 0 }, new() { Segment = 0, Multiplier = 0 }]
            }));

        private async Task<DartBotGameState> PlayUntilFinishedAsync(int gameId)
        {
            for (var visit = 0; visit < MaxVisits; visit++)
            {
                var state = await SendMissesAsync(gameId);

                if (state.Status != DartBotGameStatusEnum.Running)
                {
                    return state;
                }
            }

            throw new InvalidOperationException("Game did not finish.");
        }
    }
}
