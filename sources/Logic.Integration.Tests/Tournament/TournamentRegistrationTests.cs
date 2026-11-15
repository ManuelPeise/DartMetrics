using Logic.Integration.Tests.Infrastructure;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Interfaces.Tournament;
using Shared.Models.Shared;
using Shared.Models.Tournament;

namespace Logic.Integration.Tests.Tournament
{
    /// <summary>Anmeldung zu offiziellen Turnieren: öffnen, anmelden, bestätigen, abmelden; nur bestätigte zählen beim Start.</summary>
    [Collection(MySqlCollection.Name)]
    public class TournamentRegistrationTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public TournamentRegistrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task Player_Registers_GameMasterConfirms()
        {
            var gameMaster = await CreateUserAsync(UserRoleEnum.GameMaster);
            var player = await CreateUserAsync(UserRoleEnum.Player);

            SignIn(gameMaster, UserRoleEnum.GameMaster);
            var id = await _host.RunAsync<ILeagueService, int>(s => s.CreateLeagueAsync(new CreateLeagueModel { Name = "Offene Liga" }));

            // Vor dem Öffnen keine Anmeldung.
            SignIn(player, UserRoleEnum.Player);
            await Assert.ThrowsAsync<BusinessRuleException>(() => _host.RunAsync<ITournamentRegistrationService, int>(s => s.RegisterAsync(id)));

            SignIn(gameMaster, UserRoleEnum.GameMaster);
            await _host.RunAsync<ITournamentRegistrationService>(s => s.OpenRegistrationAsync(id, DateTime.UtcNow.AddDays(1)));

            SignIn(player, UserRoleEnum.Player);
            var participantId = await _host.RunAsync<ITournamentRegistrationService, int>(s => s.RegisterAsync(id));
            var league = await _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(id));
            Assert.Equal(TournamentStatusEnum.Registration, league.Status);
            Assert.Equal(TournamentParticipantStatusEnum.Registered, league.MyStatus);

            // Doppelt anmelden geht nicht; ein Spieler darf nicht bestätigen.
            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<ITournamentRegistrationService, int>(s => s.RegisterAsync(id)));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _host.RunAsync<ITournamentRegistrationService>(s => s.ConfirmParticipantAsync(id, participantId)));

            SignIn(gameMaster, UserRoleEnum.GameMaster);
            await _host.RunAsync<ITournamentRegistrationService>(s => s.ConfirmParticipantAsync(id, participantId));

            SignIn(player, UserRoleEnum.Player);
            league = await _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(id));
            Assert.Equal(TournamentParticipantStatusEnum.Confirmed, league.MyStatus);

            // Abmelden und erneut anmelden: wieder „angemeldet“.
            await _host.RunAsync<ITournamentRegistrationService>(s => s.WithdrawAsync(id));
            league = await _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(id));
            Assert.Null(league.MyStatus);

            Assert.Equal(participantId, await _host.RunAsync<ITournamentRegistrationService, int>(s => s.RegisterAsync(id)));
        }

        [Fact]
        public async Task PrivateTournament_HasNoRegistration()
        {
            var player = await CreateUserAsync(UserRoleEnum.Player);
            SignIn(player, UserRoleEnum.Player);

            var id = await _host.RunAsync<IKnockoutService, int>(s => s.CreateKnockoutAsync(new CreateKnockoutModel { Name = "Privat", Kind = TournamentKindEnum.Private }));

            await Assert.ThrowsAsync<BusinessRuleException>(() => _host.RunAsync<ITournamentRegistrationService>(s => s.OpenRegistrationAsync(id, null)));
        }

        private void SignIn(int userId, UserRoleEnum role) => _host.Claims.SignIn(userId, role);

        private async Task<int> CreateUserAsync(UserRoleEnum role)
        {
            var id = await _host.CreateUserAsync(role.ToString(), "Test");
            await _host.AssignRoleAsync(id, role);
            return id;
        }
    }
}
