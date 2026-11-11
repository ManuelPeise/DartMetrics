namespace Data.DartContext.Entities.User
{
    public class UserCredentialsEntity : AEntityBase
    {
        public int UserId { get; set; }

        public UserEntity User { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public DateTime PasswordChangedAt { get; set; }

        public string? RefreshTokenHash { get; set; }

        public DateTime? RefreshTokenExpiresAt { get; set; }

        public DateTime? RefreshTokenRevokedAt { get; set; }
    }
}
