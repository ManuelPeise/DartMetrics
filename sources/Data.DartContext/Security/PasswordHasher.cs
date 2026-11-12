namespace Data.DartContext.Security
{
    public static class PasswordHasher
    {
        private const int WorkFactor = 12;

        public static string HashPassword(string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);
        }

        public static bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        public static bool IsMatchingPassword(string password, string passwordReplication)
        {
            return password.Equals(passwordReplication, StringComparison.Ordinal);
        }
    }
}
