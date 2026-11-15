using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Tournament;
using Shared.Models.Bot;
using Shared.Models.Shared;
using Shared.Models.Tournament;

namespace Logic.Integration.Tests.Tournament
{
    /// <summary>
    /// Matches einer Liga spielen: Mensch gegen Bot (GameMaster gibt Darts ein, der Bot wirft sofort; FixedRandomSource = Bot trifft immer)
    /// und Bot gegen Bot (nur der Hintergrund-Takt, erst ab dem Termin). Prüft Speicherung, Tabelle, Rechte und 409 bei veraltetem Stand.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class TournamentMatchFlowTests : IAsyncLifetime
    {
        private const int MaxSteps = 500;

        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public TournamentMatchFlowTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task HumanVsBot_GameMasterEntersDarts_BotWinsAndTableCounts()
        {
            var (leagueId, playerId, gameMasterId) = await CreateRunningLeagueAsync();
            var league = await GetLeagueAsync(leagueId);
            var userParticipant = league.Participants.Single(p => p.UserId == playerId).Id;
            var match = league.Rounds.SelectMany(r => r.Matches)
                .First(m => m.HomeParticipantId == userParticipant || m.AwayParticipantId == userParticipant);

            var state = await Match(s => s.StartMatchAsync(match.Id));
            Assert.Equal(TournamentMatchStatusEnum.Running, state.Status);

            // Ein Spieler darf nicht eingeben.
            await _host.SignInNewPlayerAsync();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Match(s => s.AddDartAsync(match.Id, Miss(state))));
            _host.Claims.SignIn(gameMasterId, UserRoleEnum.GameMaster);

            // Veralteter Stand → Konflikt.
            var stale = new AddTournamentDartModel { Segment = 0, Multiplier = 0, ConcurrencyStamp = Guid.NewGuid() };
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Match(s => s.AddDartAsync(match.Id, stale)));

            for (var step = 0; step < MaxSteps && state.Status == TournamentMatchStatusEnum.Running; step++)
            {
                Assert.True(state.CanEnterDarts);
                state = await Match(s => s.AddDartAsync(match.Id, Miss(state)));
            }

            Assert.Equal(TournamentMatchStatusEnum.Finished, state.Status);
            Assert.NotEqual(userParticipant, state.WinnerParticipantId);
            Assert.Equal(3, Math.Max(state.Home.LegsWon, state.Away.LegsWon));
            Assert.Equal(0, Math.Min(state.Home.LegsWon, state.Away.LegsWon));

            // Gespeicherter Stand = berechneter Stand.
            var reloaded = await Match(s => s.GetMatchStateAsync(match.Id));
            Assert.Equal(state.Legs.Count, reloaded.Legs.Count);
            Assert.All(reloaded.Legs, l => Assert.NotNull(l.WinnerPlayer));
            Assert.False(reloaded.CanEnterDarts);

            var table = await _host.RunAsync<ILeagueService, List<LeagueTableRow>>(s => s.GetTableAsync(leagueId));
            Assert.Equal(1, table.Single(r => r.ParticipantId == userParticipant).Played);
            Assert.Equal(0, table.Single(r => r.ParticipantId == userParticipant).Points);
            Assert.Equal(2, table.Sum(r => r.Points));
        }

        [Fact]
        public async Task Undo_TakesBackHumanDart_AndBotReplyAfterIt()
        {
            var (leagueId, playerId, _) = await CreateRunningLeagueAsync();
            var league = await GetLeagueAsync(leagueId);
            var user = league.Participants.Single(p => p.UserId == playerId).Id;
            var match = league.Rounds.SelectMany(r => r.Matches).First(m => m.HomeParticipantId == user || m.AwayParticipantId == user);
            var userNumber = match.HomeParticipantId == user ? 1 : 2;

            var state = await Match(s => s.StartMatchAsync(match.Id));
            var start = Side(state, userNumber).Remaining;
            var visitsBefore = state.Legs.SelectMany(l => l.Visits).Count();

            // Volle Aufnahme (3 × S20), der Bot antwortet sofort.
            for (var i = 0; i < 3; i++)
            {
                state = await Match(s => s.AddDartAsync(match.Id, new AddTournamentDartModel { Segment = 20, Multiplier = 1, ConcurrencyStamp = state.ConcurrencyStamp }));
            }

            Assert.Equal(visitsBefore + 2, state.Legs.SelectMany(l => l.Visits).Count());
            Assert.True(state.CanUndo);

            // Zurücknehmen: dritter Dart und die Bot-Aufnahme danach sind weg, der Mensch ist wieder am Wurf.
            state = await Match(s => s.UndoLastDartAsync(match.Id, state.ConcurrencyStamp));

            Assert.Equal(userNumber, state.CurrentPlayer);
            Assert.Equal(2, state.CurrentVisit.Count);
            Assert.Equal(start - 40, Side(state, userNumber).Remaining);
            Assert.Equal(visitsBefore + 1, state.Legs.SelectMany(l => l.Visits).Count());

            // Gespeicherter Stand stimmt mit dem Neuladen überein; weiter geht es normal.
            var reloaded = await Match(s => s.GetMatchStateAsync(match.Id));
            Assert.Equal(state.CurrentVisit.Count, reloaded.CurrentVisit.Count);
            state = await Match(s => s.AddDartAsync(match.Id, new AddTournamentDartModel { Segment = 0, Multiplier = 0, ConcurrencyStamp = reloaded.ConcurrencyStamp }));
            Assert.Equal(userNumber, state.CurrentPlayer);
            Assert.Empty(state.CurrentVisit);
        }

        [Fact]
        public async Task BotVsBot_StartsOnlyAtScheduledTime_AndPlaysVisitByVisit()
        {
            var (leagueId, _, _) = await CreateRunningLeagueAsync();
            var league = await GetLeagueAsync(leagueId);
            var bots = league.Participants.Where(p => p.ParticipantType == TournamentParticipantTypeEnum.Bot).Select(p => p.Id).ToHashSet();
            var round = league.Rounds.First(r => r.Matches.Any(m => bots.Contains(m.HomeParticipantId!.Value) && bots.Contains(m.AwayParticipantId!.Value)));
            var match = round.Matches.First(m => bots.Contains(m.HomeParticipantId!.Value) && bots.Contains(m.AwayParticipantId!.Value));

            // Spielleiter darf Bot gegen Bot nicht selbst starten.
            await Assert.ThrowsAsync<BusinessRuleException>(() => Match(s => s.StartMatchAsync(match.Id)));

            var when = new DateTime(2026, 12, 1, 19, 0, 0, DateTimeKind.Utc);
            await _host.RunAsync<ILeagueService>(s => s.ScheduleRoundAsync(leagueId, round.Id, when));

            // Vor dem Termin passiert nichts.
            Assert.Equal(0, await Due(when.AddMinutes(-1)));
            Assert.Equal(TournamentMatchStatusEnum.Scheduled, (await Match(s => s.GetMatchStateAsync(match.Id))).Status);

            // Ab dem Termin: eine Aufnahme je Takt und Match.
            Assert.True(await Due(when) > 0);
            var state = await Match(s => s.GetMatchStateAsync(match.Id));
            Assert.Equal(TournamentMatchStatusEnum.Running, state.Status);
            Assert.Single(state.Legs.SelectMany(l => l.Visits));

            for (var step = 0; step < MaxSteps && state.Status == TournamentMatchStatusEnum.Running; step++)
            {
                await Due(when.AddMinutes(step));
                state = await Match(s => s.GetMatchStateAsync(match.Id));
            }

            Assert.Equal(TournamentMatchStatusEnum.Finished, state.Status);
            Assert.Equal(3, Math.Max(state.Home.LegsWon, state.Away.LegsWon));
            Assert.NotNull(state.WinnerParticipantId);
        }

        // ---------- Hilfen ----------

        private static TournamentMatchSide Side(TournamentMatchState state, int player) => player == 1 ? state.Home : state.Away;

        private static AddTournamentDartModel Miss(TournamentMatchState state) =>
            new() { Segment = 0, Multiplier = 0, ConcurrencyStamp = state.ConcurrencyStamp };

        private Task<TournamentMatchState> Match(Func<ITournamentMatchService, Task<TournamentMatchState>> action) =>
            _host.RunAsync<ITournamentMatchService, TournamentMatchState>(action);

        private Task<int> Due(DateTime now) => _host.RunAsync<ITournamentMatchService, int>(s => s.PlayDueBotMatchesAsync(now));

        private Task<TournamentOverview> GetLeagueAsync(int id) => _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(id));

        /// <summary>Liga mit 1 Spieler und 7 Bots, gestartet (Best of 5); angemeldet bleibt der GameMaster.</summary>
        private async Task<(int LeagueId, int PlayerId, int GameMasterId)> CreateRunningLeagueAsync()
        {
            var adminId = await _host.CreateUserAsync("Admin", "User");
            _host.Claims.SignIn(adminId, UserRoleEnum.Admin);
            var bots = new List<int>();
            for (var i = 0; i < 7; i++)
            {
                bots.Add(await _host.RunAsync<IDartBotAdministration, int>(a => a.CreateBotAsync(new CreateDartBotModel
                {
                    Name = $"Bot {Guid.NewGuid():N}"[..20],
                    Alias = Guid.NewGuid().ToString("N")[..6]
                })));
            }

            var playerId = await _host.CreateUserAsync("Spieler", "Test");
            await _host.AssignRoleAsync(playerId, UserRoleEnum.Player);

            var gameMasterId = await _host.CreateUserAsync("Game", "Master");
            await _host.AssignRoleAsync(gameMasterId, UserRoleEnum.GameMaster);
            _host.Claims.SignIn(gameMasterId, UserRoleEnum.GameMaster);

            var leagueId = await _host.RunAsync<ILeagueService, int>(s => s.CreateLeagueAsync(new CreateLeagueModel { Name = "Spielliga" }));
            await _host.RunAsync<ILeagueService>(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { UserId = playerId }));
            foreach (var botId in bots)
            {
                await _host.RunAsync<ILeagueService>(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await _host.RunAsync<ILeagueService>(s => s.StartLeagueAsync(leagueId, new StartLeagueModel { BestOfLegs = 5 }));
            return (leagueId, playerId, gameMasterId);
        }
    }
}
