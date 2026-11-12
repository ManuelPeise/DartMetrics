using Shared.Enums.User;

namespace Shared.Models.Blazor
{
    public class BlazorSession
    {
        public required string AccessToken { get; init; }

        public string? RefreshToken { get; init; }

        public DateTimeOffset AccessTokenExpiresAt { get; init; }

        public DateTimeOffset RefreshTokenExpiresAt { get; init; }

        public string? UserId { get; init; }

        public string? Email { get; init; }

        public UserRoleEnum[] Roles { get; init; } = [];
    }
}
