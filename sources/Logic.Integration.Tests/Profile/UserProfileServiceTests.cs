using Data.DartContext.Security;
using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.User;
using Shared.Models.Authentication;
using Shared.Models.User;

namespace Logic.Integration.Tests.Profile
{
    [Collection(MySqlCollection.Name)]
    public class UserProfileServiceTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;
        private int _userId;

        public UserProfileServiceTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _host = await TestHost.CreateAsync(_fixture);
            _userId = await _host.SignInNewPlayerAsync();
        }

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task ChangePassword_WithCorrectCurrentPassword_StoresNewHash()
        {
            await _host.RunAsync<IUserProfileService>(s => s.ChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = TestHost.TestPassword,
                Password = "Neues-Passwort-2",
                PasswordReplication = "Neues-Passwort-2"
            }));

            var hash = await _host.DbAsync(db => db.UserCredentials.Where(c => c.UserId == _userId).Select(c => c.PasswordHash).SingleAsync());
            Assert.True(PasswordHasher.VerifyPassword("Neues-Passwort-2", hash));
        }

        [Theory]
        [InlineData("falsch-falsch", "Neues-Passwort-2")]       // aktuelles Passwort falsch
        [InlineData(TestHost.TestPassword, "kurz")]             // zu kurz
        [InlineData(TestHost.TestPassword, TestHost.TestPassword)] // gleich wie bisher
        public async Task ChangePassword_Invalid_Throws(string current, string next)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<IUserProfileService>(s => s.ChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = current,
                Password = next,
                PasswordReplication = next
            })));
        }

        [Fact]
        public async Task UploadAvatar_Png_SetsVersion_AndDeleteRemovesIt()
        {
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };

            var version = await _host.RunAsync<IUserProfileService, string>(s => s.UploadAvatar(new UploadAvatarModel { Data = png }));
            var avatar = await _host.RunAsync<IUserProfileService, AvatarImage?>(s => s.GetAvatar());

            Assert.NotNull(avatar);
            Assert.Equal("image/png", avatar.ContentType);
            Assert.Equal(version, avatar.Version);

            await _host.RunAsync<IUserProfileService>(s => s.DeleteAvatar());

            var profile = await _host.RunAsync<IUserProfileService, UserProfileModel>(s => s.GetProfileData());
            Assert.Null(profile.AvatarVersion);
        }

        [Fact]
        public async Task UploadAvatar_NotAnImage_Throws()
        {
            var text = "<script>alert(1)</script>"u8.ToArray();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _host.RunAsync<IUserProfileService, string>(s => s.UploadAvatar(new UploadAvatarModel { Data = text })));
        }

        [Fact]
        public async Task UpdateProfile_EmptyFirstName_Throws()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _host.RunAsync<IUserProfileService, UserProfileModel>(s => s.UpdateUserProfile(new UserProfileModel
                {
                    FirstName = " ",
                    LastName = "Spieler",
                    EmailAddress = "x@test.local"
                })));
        }
    }
}
