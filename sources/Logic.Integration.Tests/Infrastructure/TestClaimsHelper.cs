using Data.DartContext.Entities.User;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Models.Authentication;
using System.Security.Claims;

namespace Logic.Integration.Tests.Infrastructure
{
    /// <summary>Angemeldeter Benutzer per Test umschaltbar (ersetzt den Claims-Zugriff aufs Token).</summary>
    public sealed class TestClaimsHelper : IClaimsHelper
    {
        public CurrentUser CurrentUser { get; private set; } = new() { UserId = 0, Email = "", Roles = [] };

        public void SignIn(int userId, UserRoleEnum role) =>
            CurrentUser = new CurrentUser { UserId = userId, Email = $"user{userId}@test.local", Roles = [role] };

        public CurrentUser GetCurrentUserFromClaims() => CurrentUser;

        public List<Claim> GetUserClaims(UserEntity userEntity) => throw new NotSupportedException();

        public List<Claim> GetUserClaimsFromToken(string token) => throw new NotSupportedException();
    }
}
