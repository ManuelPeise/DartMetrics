namespace Data.DartContext.Entities.User
{
    public class UserCredentialsEntity : AEntityBase
    {
        public int UserId { get; set; }

        public UserEntity User { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public DateTime PasswordChangedAt { get; set; }

        /// <summary>Einmal-Passwort (z. B. Admin-Seed aus der Konfiguration): bis zur Änderung ist nur „Passwort ändern“ erlaubt.</summary>
        public bool MustChangePassword { get; set; }

        public string? RefreshTokenHash { get; set; }

        public DateTime? RefreshTokenExpiresAt { get; set; }

        public DateTime? RefreshTokenRevokedAt { get; set; }
    }
}
