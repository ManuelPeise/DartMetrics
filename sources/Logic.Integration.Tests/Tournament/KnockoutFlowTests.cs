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
    /// K.-o.-Turnier gegen die echte Datenbank: Baum mit Freilosen, Weiterrücken bis zum Sieger,
    /// Setzliste aus einer abgeschlossenen Liga. Matches werden hier direkt als beendet eingetragen (Spielen folgt in Phase 4/5).
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class KnockoutFlowTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public KnockoutFlowTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task SixParticipants_ByesAdvance_AndFinalDecidesWinner()
        {
            var bots = await CreateBotsAsync(6);
            await SignInGameMasterAsync();

            var id = await _host.RunAsync<IKnockoutService, int>(s => s.CreateKnockoutAsync(new CreateKnockoutModel { Name = "Cup" }));
            foreach (var botId in bots)
            {
                await _host.RunAsync<IKnockoutService>(s => s.AddParticipantAsync(id, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await _host.RunAsync<IKnockoutService>(s => s.StartKnockoutAsync(id, new StartKnockoutModel { BestOfLegsPerRound = [5, 7, 9] }));

            var cup = await GetAsync(id);
            Assert.Equal(TournamentStatusEnum.Running, cup.Status);
            Assert.Equal(["Viertelfinale", "Halbfinale", "Finale"], cup.Rounds.Select(r => r.Name));
            Assert.Equal([5, 7, 9], cup.Rounds.Select(r => r.BestOfLegs));
            Assert.Equal([4, 2, 1], cup.Rounds.Select(r => r.Matches.Count));

            // 6 in einem 8er-Baum: 2 Freilose, deren Gewinner stehen schon im Halbfinale.
            var byes = cup.Rounds[0].Matches.Where(m => m.Status == TournamentMatchStatusEnum.Bye).ToList();
            Assert.Equal(2, byes.Count);
            var semiSides = cup.Rounds[1].Matches.SelectMany(m => new[] { m.HomeParticipantId, m.AwayParticipantId }).Where(x => x is not null).ToList();
            Assert.All(byes, b => Assert.Contains(b.WinnerParticipantId, semiSides));

            // Runde für Runde: Heim gewinnt.
            for (var round = 0; round < 3; round++)
            {
                foreach (var match in cup.Rounds[round].Matches.Where(m => m.Status == TournamentMatchStatusEnum.Scheduled))
                {
                    await FinishMatchAsync(match.Id, homeWins: true, legsToWin: (cup.Rounds[round].BestOfLegs / 2) + 1);
                }

                await _host.RunAsync<IKnockoutService>(s => s.AdvanceAsync(id));
                cup = await GetAsync(id);
            }

            var final = cup.Rounds[2].Matches.Single();
            Assert.Equal(TournamentStatusEnum.Finished, cup.Status);
            Assert.Equal(final.HomeParticipantId, cup.WinnerParticipantId);

            // Wiederholt aufrufen ändert nichts.
            await _host.RunAsync<IKnockoutService>(s => s.AdvanceAsync(id));
            Assert.Equal(cup.WinnerParticipantId, (await GetAsync(id)).WinnerParticipantId);
        }

        [Fact]
        public async Task SeedsFromFinishedLeague_TopFourNeverMeetInFirstRound()
        {
            var leagueBots = await CreateBotsAsync(8);
            var extraBots = await CreateBotsAsync(4);
            await SignInGameMasterAsync();

            var leagueId = await _host.RunAsync<ILeagueService, int>(s => s.CreateLeagueAsync(new CreateLeagueModel { Name = "Liga" }));
            foreach (var botId in leagueBots)
            {
                await _host.RunAsync<ILeagueService>(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await _host.RunAsync<ILeagueService>(s => s.StartLeagueAsync(leagueId, new StartLeagueModel()));

            // Kleinere Teilnehmer-Id gewinnt immer 3:0 → eindeutige Tabelle.
            var league = await _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(leagueId));
            foreach (var match in league.Rounds.SelectMany(r => r.Matches))
            {
                await FinishMatchAsync(match.Id, homeWins: match.HomeParticipantId < match.AwayParticipantId, legsToWin: 3);
            }

            Assert.True(await _host.RunAsync<ILeagueService, bool>(s => s.FinishLeagueIfCompleteAsync(leagueId)));
            var table = await _host.RunAsync<ILeagueService, List<LeagueTableRow>>(s => s.GetTableAsync(leagueId));
            var topBotIds = table.Take(4)
                .Select(row => league.Participants.Single(p => p.Id == row.ParticipantId).DartBotId!.Value)
                .ToList();

            var cupId = await _host.RunAsync<IKnockoutService, int>(s => s.CreateKnockoutAsync(new CreateKnockoutModel
            {
                Name = "WM",
                SeedSourceTournamentId = leagueId,
                SeedCount = 4
            }));

            foreach (var botId in extraBots)
            {
                await _host.RunAsync<IKnockoutService>(s => s.AddParticipantAsync(cupId, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await _host.RunAsync<IKnockoutService>(s => s.StartKnockoutAsync(cupId, new StartKnockoutModel()));
            var cup = await GetAsync(cupId);

            // Die Top 4 der Liga wurden ergänzt und gesetzt.
            Assert.Equal(8, cup.Participants.Count);
            Assert.Equal(topBotIds, cup.Participants.Where(p => p.Seed is not null).OrderBy(p => p.Seed).Select(p => p.DartBotId!.Value));
            Assert.All(cup.Rounds, r => Assert.Equal(5, r.BestOfLegs));

            var seededIds = cup.Participants.Where(p => p.Seed is not null).Select(p => p.Id).ToHashSet();
            Assert.All(cup.Rounds[0].Matches, m =>
                Assert.Single(new[] { m.HomeParticipantId!.Value, m.AwayParticipantId!.Value }, seededIds.Contains));
        }

        [Fact]
        public async Task SeedSource_MustBeFinished()
        {
            var bots = await CreateBotsAsync(8);
            await SignInGameMasterAsync();

            var leagueId = await _host.RunAsync<ILeagueService, int>(s => s.CreateLeagueAsync(new CreateLeagueModel { Name = "Liga" }));
            var cupId = await _host.RunAsync<IKnockoutService, int>(s => s.CreateKnockoutAsync(new CreateKnockoutModel { Name = "WM", SeedSourceTournamentId = leagueId }));

            foreach (var botId in bots)
            {
                await _host.RunAsync<IKnockoutService>(s => s.AddParticipantAsync(cupId, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await Assert.ThrowsAsync<InvalidOperationException>(() => _host.RunAsync<IKnockoutService>(s => s.StartKnockoutAsync(cupId, new StartKnockoutModel())));
        }

        [Fact]
        public async Task WrongNumberOfSetLengths_IsRejected()
        {
            var bots = await CreateBotsAsync(4);
            await SignInGameMasterAsync();
            var id = await _host.RunAsync<IKnockoutService, int>(s => s.CreateKnockoutAsync(new CreateKnockoutModel { Name = "Cup" }));
            foreach (var botId in bots)
            {
                await _host.RunAsync<IKnockoutService>(s => s.AddParticipantAsync(id, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<IKnockoutService>(s =>
                s.StartKnockoutAsync(id, new StartKnockoutModel { BestOfLegsPerRound = [5, 7, 9] })));
        }

        // ---------- Hilfen ----------

        private Task<TournamentOverview> GetAsync(int id) => _host.RunAsync<IKnockoutService, TournamentOverview>(s => s.GetKnockoutAsync(id));

        private Task FinishMatchAsync(int matchId, bool homeWins, int legsToWin) =>
            _host.DbAsync(async db =>
            {
                var match = await db.Set<TournamentMatchEntity>().SingleAsync(m => m.Id == matchId);
                match.Status = TournamentMatchStatusEnum.Finished;
                match.StartedAt = DateTime.UtcNow.AddMinutes(-30);
                match.FinishedAt = DateTime.UtcNow;
                match.HomeLegs = homeWins ? legsToWin : 0;
                match.AwayLegs = homeWins ? 0 : legsToWin;
                match.WinnerParticipantId = homeWins ? match.HomeParticipantId : match.AwayParticipantId;
                match.UpdatedAt = DateTime.UtcNow;
                match.UpdatedBy = "tests";
                match.ConcurrencyStamp = Guid.NewGuid();
                return await db.SaveChangesAsync();
            });

        private async Task SignInGameMasterAsync()
        {
            var id = await _host.CreateUserAsync("Game", "Master");
            await _host.AssignRoleAsync(id, UserRoleEnum.GameMaster);
            _host.Claims.SignIn(id, UserRoleEnum.GameMaster);
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
