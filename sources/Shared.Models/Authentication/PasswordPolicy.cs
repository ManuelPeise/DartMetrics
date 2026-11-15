namespace Shared.Models.Authentication
{
    /// <summary>Passwortregeln für Registrierung und Passwortänderung (Server prüft, Client zeigt sie an).</summary>
    public static class PasswordPolicy
    {
        public const int MinLength = 8;

        /// <summary>BCrypt verarbeitet nur die ersten 72 Bytes.</summary>
        public const int MaxLength = 72;

        public static string Description => $"Mindestens {MinLength} Zeichen.";

        public static void Validate(string password, string parameterName)
        {
            if (password.Length < MinLength || System.Text.Encoding.UTF8.GetByteCount(password) > MaxLength)
            {
                throw new ArgumentException($"Password must be between {MinLength} characters and {MaxLength} bytes.", parameterName);
            }
        }
    }
}
