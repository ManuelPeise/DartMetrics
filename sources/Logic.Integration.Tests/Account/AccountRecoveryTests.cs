using System.Text.RegularExpressions;
using Data.DartContext.Entities.Tournament;
using Data.DartContext.Security;
using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Tournament;
using Shared.Interfaces.User;
using Shared.Models.Authentication;
using Shared.Models.Bot;
using Shared.Models.Options;
using Shared.Models.Tournament;

namespace Logic.Integration.Tests.Account
{
    /// <summary>Passwort vergessen/zurücksetzen, Bestätigungsmail erneut senden, Konto selbst löschen (inkl. Turniere) und Löschen durch den Admin.</summary>
    [Collection(MySqlCollection.Name)]
    public class AccountRecoveryTests : IAsyncLifetime
    {
        private static readonly JwtOptions Jwt = new() { Key = "integration-test-key-with-at-least-64-characters-0123456789abcdef", Issuer = "tests", Audience = "tests" };
        private static readonly ServerOptions Server = new() { Host = "https://localhost", Port = 5001 };

        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public AccountRecoveryTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task PasswordReset_WorksOnce_AndLogsOutEverywhere()
        {
            var (userId, email) = await CreateUserAsync();

            await Registration(r => r.RequestPasswordReset(email, Jwt, Server));
            var token = TokenFrom(Assert.Single(_host.Mails.Sent, m => m.ToAddress == email).Body);

            // Zweite Anfrage direkt danach: keine weitere Mail (Sperre gegen Spam).
            await Registration(r => r.RequestPasswordReset(email, Jwt, Server));
            Assert.Single(_host.Mails.Sent, m => m.ToAddress == email);

            var model = new ResetPasswordRequestModel { Token = token, Password = "Neues-Passwort-1", PasswordReplication = "Neues-Passwort-1" };
            Assert.True(await _host.RunAsync<IUserRegistration, bool>(r => r.ResetPassword(model, Jwt)));

            var credentials = await _host.DbAsync(db => db.UserCredentials.SingleAsync(c => c.UserId == userId));
            Assert.True(PasswordHasher.VerifyPassword("Neues-Passwort-1", credentials.PasswordHash));
            Assert.NotNull(credentials.RefreshTokenRevokedAt);
            Assert.Null(credentials.RefreshTokenHash);

            // Derselbe Link ein zweites Mal: wertlos.
            var again = new ResetPasswordRequestModel { Token = token, Password = "Drittes-Passwort-1", PasswordReplication = "Drittes-Passwort-1" };
            Assert.False(await _host.RunAsync<IUserRegistration, bool>(r => r.ResetPassword(again, Jwt)));
        }

        [Fact]
        public async Task PasswordReset_RejectsTamperedToken_AndUnknownAddress()
        {
            await Registration(r => r.RequestPasswordReset($"{Guid.NewGuid():N}@nowhere.local", Jwt, Server));
            Assert.Empty(_host.Mails.Sent);

            var model = new ResetPasswordRequestModel { Token = "not-a-token", Password = "Neues-Passwort-1", PasswordReplication = "Neues-Passwort-1" };
            Assert.False(await _host.RunAsync<IUserRegistration, bool>(r => r.ResetPassword(model, Jwt)));

            var tooShort = new ResetPasswordRequestModel { Token = "x", Password = "kurz", PasswordReplication = "kurz" };
            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<IUserRegistration, bool>(r => r.ResetPassword(tooShort, Jwt)));
        }

        [Fact]
        public async Task ResendVerification_OnlyForUnverified()
        {
            var (verifiedId, verifiedEmail) = await CreateUserAsync();
            var (unverifiedId, unverifiedEmail) = await CreateUserAsync();
            await _host.DbAsync(async db =>
            {
                var user = await db.Users.SingleAsync(u => u.Id == unverifiedId);
                user.EmailVerifiedAt = null;
                return await db.SaveChangesAsync();
            });

            await Registration(r => r.ResendVerificationMail(verifiedEmail, Jwt, Server));
            await Registration(r => r.ResendVerificationMail(unverifiedEmail, Jwt, Server));

            Assert.DoesNotContain(_host.Mails.Sent, m => m.ToAddress == verifiedEmail);
            Assert.Contains(_host.Mails.Sent, m => m.ToAddress == unverifiedEmail);
            Assert.NotEqual(verifiedId, unverifiedId);
        }

        [Fact]
        public async Task DeleteOwnAccount_WrongPassword_IsRejected()
        {
            var (userId, _) = await CreateUserAsync();
            _host.Claims.SignIn(userId, UserRoleEnum.Player);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _host.RunAsync<IUserProfileService>(s => s.DeleteOwnAccount(new DeleteOwnAccountModel { Password = "falsch-falsch" })));
        }

        [Fact]
        public async Task DeleteOwnAccount_AnonymizesAndForfeitsLeagueMatches()
        {
            var (leagueId, playerId, gameMasterId) = await CreateRunningLeagueAsync();

            _host.Claims.SignIn(playerId, UserRoleEnum.Player);
            await _host.RunAsync<IUserProfileService>(s => s.DeleteOwnAccount(new DeleteOwnAccountModel { Password = TestHost.TestPassword }));

            var user = await _host.DbAsync(db => db.Users.Include(u => u.UserCredentials).SingleAsync(u => u.Id == playerId));
            Assert.NotNull(user.DeletedAt);
            Assert.Null(user.UserCredentials);
            Assert.StartsWith("deleted-", user.EmailAddress);

            _host.Claims.SignIn(gameMasterId, UserRoleEnum.GameMaster);
            var league = await _host.RunAsync<ILeagueService, TournamentOverview>(s => s.GetLeagueAsync(leagueId));
            var participant = league.Participants.Single(p => p.UserId == playerId);
            Assert.Equal("Gelöschter Spieler", participant.DisplayName);

            // Alle 14 Matches des Gelöschten sind kampflos an den Gegner gegangen.
            var own = league.Rounds.SelectMany(r => r.Matches).Where(m => m.HomeParticipantId == participant.Id || m.AwayParticipantId == participant.Id).ToList();
            Assert.Equal(14, own.Count);
            Assert.All(own, m => Assert.Equal(TournamentMatchStatusEnum.Finished, m.Status));
            Assert.All(own, m => Assert.NotEqual(participant.Id, m.WinnerParticipantId));

            var table = await _host.RunAsync<ILeagueService, List<LeagueTableRow>>(s => s.GetTableAsync(leagueId));
            Assert.Equal(0, table.Single(r => r.ParticipantId == participant.Id).Points);
        }

        [Fact]
        public async Task AdminDelete_WithdrawsFromTournamentNotStarted()
        {
            var (playerId, _) = await CreateUserAsync();
            var gameMasterId = await CreateUserWithRoleAsync(UserRoleEnum.GameMaster);
            _host.Claims.SignIn(gameMasterId, UserRoleEnum.GameMaster);

            var leagueId = await _host.RunAsync<ILeagueService, int>(s => s.CreateLeagueAsync(new CreateLeagueModel { Name = "Noch nicht gestartet" }));
            await _host.RunAsync<ILeagueService>(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { UserId = playerId }));

            var adminId = await CreateUserWithRoleAsync(UserRoleEnum.Admin);
            _host.Claims.SignIn(adminId, UserRoleEnum.Admin);
            await _host.RunAsync<IUserAdministration>(a => a.DeleteUserAsync(playerId));

            var status = await _host.DbAsync(db => db.Set<TournamentParticipantEntity>().Where(p => p.UserId == playerId).Select(p => p.Status).SingleAsync());
            Assert.Equal(TournamentParticipantStatusEnum.Withdrawn, status);
        }

        [Fact]
        public async Task ChangePassword_ClearsOneTimePasswordFlag()
        {
            var (userId, _) = await CreateUserAsync();
            await _host.DbAsync(async db =>
            {
                var credentials = await db.UserCredentials.SingleAsync(c => c.UserId == userId);
                credentials.MustChangePassword = true;
                return await db.SaveChangesAsync();
            });

            _host.Claims.SignIn(userId, UserRoleEnum.Player);
            await _host.RunAsync<IUserProfileService>(s => s.ChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = TestHost.TestPassword,
                Password = "Eigenes-Passwort-1",
                PasswordReplication = "Eigenes-Passwort-1"
            }));

            var after = await _host.DbAsync(db => db.UserCredentials.SingleAsync(c => c.UserId == userId));
            Assert.False(after.MustChangePassword);
        }

        // ---------- Hilfen ----------

        private Task Registration(Func<IUserRegistration, Task> action) => _host.RunAsync(action);

        private static string TokenFrom(string body) => Uri.UnescapeDataString(Regex.Match(body, "token=([^'\"]+)").Groups[1].Value);

        private async Task<(int Id, string Email)> CreateUserAsync()
        {
            var id = await CreateUserWithRoleAsync(UserRoleEnum.Player);
            var email = await _host.DbAsync(db => db.Users.Where(u => u.Id == id).Select(u => u.EmailAddress).SingleAsync());
            return (id, email);
        }

        private async Task<int> CreateUserWithRoleAsync(UserRoleEnum role)
        {
            var id = await _host.CreateUserAsync(role.ToString(), "Test");
            await _host.AssignRoleAsync(id, role);
            return id;
        }

        /// <summary>Liga mit 1 Spieler und 7 Bots, gestartet; kein Match gespielt.</summary>
        private async Task<(int LeagueId, int PlayerId, int GameMasterId)> CreateRunningLeagueAsync()
        {
            var adminId = await CreateUserWithRoleAsync(UserRoleEnum.Admin);
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

            var (playerId, _) = await CreateUserAsync();
            var gameMasterId = await CreateUserWithRoleAsync(UserRoleEnum.GameMaster);
            _host.Claims.SignIn(gameMasterId, UserRoleEnum.GameMaster);

            var leagueId = await _host.RunAsync<ILeagueService, int>(s => s.CreateLeagueAsync(new CreateLeagueModel { Name = "Liga" }));
            await _host.RunAsync<ILeagueService>(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { UserId = playerId }));
            foreach (var botId in bots)
            {
                await _host.RunAsync<ILeagueService>(s => s.AddParticipantAsync(leagueId, new AddTournamentParticipantModel { DartBotId = botId }));
            }

            await _host.RunAsync<ILeagueService>(s => s.StartLeagueAsync(leagueId, new StartLeagueModel()));
            return (leagueId, playerId, gameMasterId);
        }
    }
}
