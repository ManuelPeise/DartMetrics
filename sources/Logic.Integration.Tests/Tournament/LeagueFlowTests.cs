using Data.DartContext.Entities.Tournament;
using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Tournament;
using Shared.Models.Bot;
using Shared.Models.Tournament;

namespace Logic.Integration.Tests.Tournament
{
    /// <summary>
    /// Liga gegen die echte Datenbank: GameMaster legt an, besetzt mit Spielern und Bots, startet (Spielplan Hin- und Rückrunde),
    /// terminiert einen Spieltag. Prüft Rechte, Grenzen und die CHECK-Constraints der Turnier-Tabellen.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class LeagueFlowTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public LeagueFlowTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task GameMaster_StartsLeague_WithDoubleRoundRobin()
        {
            var players = await CreatePlayersAsync(4);
            var bots = await CreateBotsAsync(5);
            await SignInGameMasterAsync();

            var leagueId = await CreateLeagueAsync();

            foreach (var userId in players)
            {
                await Run(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { UserId = userId }));
            }

            foreach (var botId in bots)
            {
                await Run(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await Run(s => s.StartLeagueAsync(leagueId, new StartLeagueModel { BestOfLegs = 7 }));

            var league = await _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(leagueId));

            // 9 Teilnehmer: 10 Plätze → 2 × 9 Spieltage mit je 4 Matches, einer spielfrei.
            Assert.Equal(TournamentStatusEnum.Running, league.Status);
            Assert.Equal(9, league.Participants.Count);
            Assert.Equal(18, league.Rounds.Count);
            Assert.All(league.Rounds, r => Assert.Equal(4, r.Matches.Count));
            Assert.All(league.Rounds, r => Assert.Equal(7, r.BestOfLegs));
            Assert.Equal(9 * 8, league.Rounds.Sum(r => r.Matches.Count));

            var table = await _host.RunAsync<ILeagueService, List<LeagueTableRow>>(s => s.GetTableAsync(leagueId));
            Assert.Equal(9, table.Count);
            Assert.All(table, row => Assert.Equal(0, row.Played));

            var when = new DateTime(2026, 12, 1, 19, 0, 0, DateTimeKind.Utc);
            await Run(s => s.ScheduleRoundAsync(leagueId, league.Rounds[0].Id, when));
            league = await _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(leagueId));
            Assert.Equal(when, league.Rounds[0].ScheduledAt);

            Assert.False(await _host.RunAsync<ILeagueService, bool>(s => s.FinishLeagueIfCompleteAsync(leagueId)));

            // Nach dem Start keine Änderungen an der Besetzung.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Run(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = bots[0] })));
        }

        [Fact]
        public async Task Start_WithTooFewParticipants_IsRejected()
        {
            var bots = await CreateBotsAsync(7);
            await SignInGameMasterAsync();
            var leagueId = await CreateLeagueAsync();

            foreach (var botId in bots)
            {
                await Run(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await Assert.ThrowsAsync<ArgumentException>(() => Run(s => s.StartLeagueAsync(leagueId, new StartLeagueModel())));
        }

        [Fact]
        public async Task SameBotTwice_IsRejected_WithdrawnCanReturn()
        {
            var bots = await CreateBotsAsync(1);
            await SignInGameMasterAsync();
            var leagueId = await CreateLeagueAsync();

            var participantId = await _host.RunAsync<ILeagueService, int>(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = bots[0] }));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                Run(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = bots[0] })));

            await Run(s => s.RemoveParticipantAsync(leagueId, participantId));
            var again = await _host.RunAsync<ILeagueService, int>(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = bots[0] }));

            Assert.Equal(participantId, again);
        }

        [Fact]
        public async Task Player_CannotCreateOrManageOfficialLeague()
        {
            await SignInGameMasterAsync();
            var leagueId = await CreateLeagueAsync();

            await _host.SignInNewPlayerAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _host.RunAsync<ILeagueService, int>(s =>
                s.CreateLeagueAsync(new CreateLeagueModel { Name = "Nicht erlaubt", Kind = TournamentKindEnum.Official })));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                Run(s => s.StartLeagueAsync(leagueId, new StartLeagueModel())));

            // Ansehen darf jeder angemeldete Benutzer.
            var league = await _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(leagueId));
            Assert.Equal(leagueId, league.Id);
        }

        [Fact]
        public async Task PrivateLeague_OnlyOwnerAndBots()
        {
            var otherPlayer = (await CreatePlayersAsync(1))[0];
            var bots = await CreateBotsAsync(1);
            var ownerId = await _host.SignInNewPlayerAsync();
            await _host.AssignRoleAsync(ownerId, UserRoleEnum.Player);

            var leagueId = await _host.RunAsync<ILeagueService, int>(s =>
                s.CreateLeagueAsync(new CreateLeagueModel { Name = "Meine Liga", Kind = TournamentKindEnum.Private }));

            await Run(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { UserId = ownerId }));
            await Run(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = bots[0] }));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                Run(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { UserId = otherPlayer })));

            await _host.SignInNewPlayerAsync();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(leagueId)));
        }

        [Fact]
        public async Task CheckConstraint_RejectsParticipantWithUserAndBot()
        {
            var userId = (await CreatePlayersAsync(1))[0];
            var botId = (await CreateBotsAsync(1))[0];
            await SignInGameMasterAsync();
            var leagueId = await CreateLeagueAsync();

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => _host.DbAsync(async db =>
            {
                db.Set<TournamentParticipantEntity>().Add(new TournamentParticipantEntity
                {
                    TournamentId = leagueId,
                    ParticipantType = TournamentParticipantTypeEnum.Bot,
                    UserId = userId,
                    DartBotId = botId,
                    DisplayName = "Kaputt",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "tests"
                });
                return await db.SaveChangesAsync();
            }));
        }

        // ---------- Hilfen ----------

        private Task Run(Func<ILeagueService, Task> action) => _host.RunAsync(action);

        private Task<int> CreateLeagueAsync() =>
            _host.RunAsync<ILeagueService, int>(s => s.CreateLeagueAsync(new CreateLeagueModel { Name = "Testliga" }));

        private async Task SignInGameMasterAsync()
        {
            var id = await _host.CreateUserAsync("Game", "Master");
            await _host.AssignRoleAsync(id, UserRoleEnum.GameMaster);
            _host.Claims.SignIn(id, UserRoleEnum.GameMaster);
        }

        private async Task<List<int>> CreatePlayersAsync(int count)
        {
            var ids = new List<int>();

            for (var i = 0; i < count; i++)
            {
                var id = await _host.CreateUserAsync($"Spieler{i}", "Test");
                await _host.AssignRoleAsync(id, UserRoleEnum.Player);
                ids.Add(id);
            }

            return ids;
        }

        private async Task<List<int>> CreateBotsAsync(int count)
        {
            var adminId = await _host.CreateUserAsync("Admin", "User");
            _host.Claims.SignIn(adminId, UserRoleEnum.Admin);
            var ids = new List<int>();

            for (var i = 0; i < count; i++)
            {
                ids.Add(await _host.RunAsync<IDartBotAdministration, int>(a => a.CreateBotAsync(new CreateDartBotModel
                {
                    Name = $"Bot {Guid.NewGuid():N}"[..20],
                    Alias = Guid.NewGuid().ToString("N")[..6]
                })));
            }

            return ids;
        }
    }
}
