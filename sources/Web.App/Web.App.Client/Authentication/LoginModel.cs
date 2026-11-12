using System.ComponentModel.DataAnnotations;

namespace Web.App.Client.Authentication
{
    public sealed class LoginModel
    {
        [Required(ErrorMessage = "Bitte E-Mail-Adresse eingeben.")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte Passwort eingeben.")]
        public string Password { get; set; } = string.Empty;
    }
}
