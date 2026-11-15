using System.Text.RegularExpressions;
using Data.DartContext.Entities.User;
using Logic.Administration.DataRetention;
using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Models.Authentication;
using Shared.Models.Options;
using Shared.Models.User;

namespace Logic.Integration.Tests.Account
{
    /// <summary>
    /// Sicherheitspfade: Registrierung, E-Mail-Bestätigung, Login, Refresh-Token-Rotation und die Löschfristen
    /// für nicht bestätigte Konten (Review-Findings T2 und A5).
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class LoginAndRegistrationTests : IAsyncLifetime
    {
        private static readonly JwtOptions Jwt = new()
        {
            Key = "integration-test-key-with-at-least-64-characters-0123456789abcdef",
            Issuer = "tests",
            Audience = "tests",
            ExpireMinutes = 30,
            RefreshTokenExpireDays = 60
        };

        private static readonly ServerOptions Server = new() { Host = "https://localhost", Port = 5001 };

        private const string Password = "Registrierung-1";

        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public LoginAndRegistrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        // ---------- Registrierung + Bestätigung ----------

        [Fact]
        public async Task Register_ThenVerify_ThenLogin_Works()
        {
            var email = NewEmail();

            Assert.True(await Register(email));

            // Vor der Bestätigung: kein Login.
            Assert.Null(await Login(email, Password));

            var token = TokenFrom(Assert.Single(_host.Mails.Sent, m => m.ToAddress == email).Body);
            Assert.True(await _host.RunAsync<IUserRegistration, bool>(r => r.VerifyEmail(token, Jwt, Server)));

            var tokens = await Login(email, Password);
            Assert.NotNull(tokens);
            Assert.False(string.IsNullOrEmpty(tokens!.Token));
            Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));

            // Refresh-Token liegt nur als Hash in der Datenbank.
            var credentials = await _host.DbAsync(db => db.UserCredentials.SingleAsync(c => c.User.EmailAddress == email));
            Assert.NotEqual(tokens.RefreshToken, credentials.RefreshTokenHash);
        }

        [Fact]
        public async Task Register_RejectsWeakOrMismatchingPassword()
        {
            Assert.False(await Register(NewEmail(), "kurz", "kurz"));
            Assert.False(await Register(NewEmail(), Password, Password + "x"));
            Assert.Empty(_host.Mails.Sent);
        }

        [Fact]
        public async Task Register_WithVerifiedEmail_Fails()
        {
            var userId = await _host.CreateUserAsync();
            var email = await EmailOf(userId);

            Assert.False(await Register(email));
            Assert.DoesNotContain(_host.Mails.Sent, m => m.ToAddress == email);
        }

        [Fact]
        public async Task VerifyEmail_RejectsTamperedToken()
        {
            Assert.False(await _host.RunAsync<IUserRegistration, bool>(r => r.VerifyEmail("not-a-token", Jwt, Server)));
        }

        // ---------- Login ----------

        [Fact]
        public async Task Login_WrongPassword_OrUnknownUser_ReturnsNull_WithoutLogEntry()
        {
            var userId = await _host.CreateUserAsync();
            var email = await EmailOf(userId);
            var logsBefore = await _host.DbAsync(db => db.LogMessages.CountAsync());

            Assert.Null(await Login(email, "falsches-Passwort"));
            Assert.Null(await Login(NewEmail(), Password));

            // Fehlversuche sind kein Fehler: kein Eintrag im MessageLog (Schutz gegen Log-Flut bei Brute Force).
            Assert.Equal(logsBefore, await _host.DbAsync(db => db.LogMessages.CountAsync()));
        }

        [Fact]
        public async Task Login_InactiveUser_IsRejected()
        {
            var userId = await _host.CreateUserAsync();
            var email = await EmailOf(userId);
            await _host.DbAsync(db => db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));

            Assert.Null(await Login(email, TestHost.TestPassword));
        }

        // ---------- Refresh-Token ----------

        [Fact]
        public async Task RefreshToken_Rotates_AndOldTokenIsInvalid()
        {
            var userId = await _host.CreateUserAsync();
            await _host.AssignRoleAsync(userId, UserRoleEnum.Player);
            var tokens = await Login(await EmailOf(userId), TestHost.TestPassword);
            Assert.NotNull(tokens);

            var refreshed = await Refresh(tokens!.RefreshToken!);
            Assert.NotNull(refreshed);
            Assert.NotEqual(tokens.RefreshToken, refreshed!.RefreshToken);

            // Der alte Refresh-Token ist nach der Rotation wertlos.
            Assert.Null(await Refresh(tokens.RefreshToken!));
            Assert.Null(await Refresh("unknown-refresh-token"));
        }

        // ---------- Löschfristen ----------

        [Fact]
        public async Task DataRetention_DeletesOnlyOldUnverifiedAccounts()
        {
            var options = new DataRetentionOptions { UnverifiedAccountDays = 7, LogMessageDays = 90 };

            var oldUnverified = await CreateUnverifiedAsync(daysAgo: 10);
            var newUnverified = await CreateUnverifiedAsync(daysAgo: 2);
            var verified = await _host.CreateUserAsync();
            await _host.DbAsync(db => db.Users.Where(u => u.Id == verified).ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, DateTime.UtcNow.AddDays(-30))));

            var (accounts, _) = await _host.DbAsync(db => DataRetentionWorker.RunOnceAsync(db, options, DateTime.UtcNow));

            Assert.True(accounts >= 1);
            var remaining = await _host.DbAsync(db => db.Users.Where(u => u.Id == oldUnverified || u.Id == newUnverified || u.Id == verified).Select(u => u.Id).ToListAsync());
            Assert.DoesNotContain(oldUnverified, remaining);
            Assert.Contains(newUnverified, remaining);
            Assert.Contains(verified, remaining);

            // Anmeldedaten sind per Cascade mitgelöscht.
            Assert.False(await _host.DbAsync(db => db.UserCredentials.AnyAsync(c => c.UserId == oldUnverified)));
        }

        // ---------- Hilfen ----------

        private static string NewEmail() => $"{Guid.NewGuid():N}@test.local";

        private static string TokenFrom(string body) => Uri.UnescapeDataString(Regex.Match(body, "token=([^'\"]+)").Groups[1].Value);

        private Task<string> EmailOf(int userId) =>
            _host.DbAsync(db => db.Users.Where(u => u.Id == userId).Select(u => u.EmailAddress).SingleAsync());

        private Task<bool> Register(string email, string password = Password, string? replication = null) =>
            _host.RunAsync<IUserRegistration, bool>(r => r.RegisterUser(new UserRegistrationRequestModel
            {
                FirstName = "Neu",
                LastName = "Spieler",
                EmailAddress = email,
                UserCredentials = new UserCredentialsModelBase { Password = password, PasswordReplication = replication ?? password }
            }, Jwt, Server));

        private Task<TokenResponse?> Login(string email, string password) =>
            _host.RunAsync<IUserAuthentication, TokenResponse?>(a => a.AuthenticateUser(new AuthenticationRequest { EmailAddress = email, Password = password }, Jwt));

        private Task<TokenResponse?> Refresh(string refreshToken) =>
            _host.RunAsync<IUserAuthentication, TokenResponse?>(a => a.RefreshToken(new RefreshTokenRequest { RefreshToken = refreshToken }, Jwt));

        private async Task<int> CreateUnverifiedAsync(int daysAgo)
        {
            var userId = await _host.CreateUserAsync();
            var createdAt = DateTime.UtcNow.AddDays(-daysAgo);

            // Direkt per SQL: SaveChanges würde CreatedAt/UpdatedAt neu setzen.
            await _host.DbAsync(db => db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s
                .SetProperty(u => u.EmailVerifiedAt, (DateTime?)null)
                .SetProperty(u => u.IsActive, false)
                .SetProperty(u => u.CreatedAt, createdAt)
                .SetProperty(u => u.UpdatedAt, (DateTime?)null)));

            return userId;
        }
    }
}
