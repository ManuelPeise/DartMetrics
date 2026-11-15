namespace Web.Shared.Constants
{
    public static class BlazorClaimTypes
    {
        public const string SessionId = "dartmetrics:session_id";
        public const string UserId = "UserId";
        public const string UserEmail = "UserEmail";
        public const string UserRoles = "UserRoles";

        /// <summary>Einmal-Passwort: bis zur Änderung nur "Passwort ändern" (Claim aus dem Access-Token, auch im Cookie).</summary>
        public const string MustChangePassword = "MustChangePassword";
    }
}
