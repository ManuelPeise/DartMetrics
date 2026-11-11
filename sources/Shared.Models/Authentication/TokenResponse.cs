namespace Shared.Models.Authentication
{
    public class TokenResponse
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
