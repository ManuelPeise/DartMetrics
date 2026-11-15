namespace Shared.Models.Authentication
{
    /// <summary>Nur eine E-Mail-Adresse (Passwort vergessen, Bestätigungsmail erneut senden).</summary>
    public class EmailAddressRequestModel
    {
        public string EmailAddress { get; set; } = "";
    }

    /// <summary>Neues Passwort mit dem Token aus der Reset-Mail.</summary>
    public class ResetPasswordRequestModel
    {
        public string Token { get; set; } = "";

        public string Password { get; set; } = "";

        public string PasswordReplication { get; set; } = "";
    }

    /// <summary>Konto selbst löschen: Bestätigung mit dem aktuellen Passwort.</summary>
    public class DeleteOwnAccountModel
    {
        public string Password { get; set; } = "";
    }
}
