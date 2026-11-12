namespace Shared.Models.Authentication
{
    public sealed class RefreshTokenRequest
    {
        public required string RefreshToken { get; init; }
    }
}
