using System.ComponentModel.DataAnnotations;

namespace Web.App.Client.Authentication
{
    public sealed class RegisterModel
    {
        [Required(ErrorMessage = "Bitte Vornamen eingeben.")]
        [StringLength(100, ErrorMessage = "Maximal 100 Zeichen.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte Nachnamen eingeben.")]
        [StringLength(100, ErrorMessage = "Maximal 100 Zeichen.")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Maximal 50 Zeichen.")]
        public string? DisplayName { get; set; }

        [Required(ErrorMessage = "Bitte E-Mail-Adresse eingeben.")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte Passwort eingeben.")]
        [StringLength(72, MinimumLength = 8, ErrorMessage = "8 bis 72 Zeichen.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte Passwort wiederholen.")]
        [Compare(nameof(Password), ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string PasswordReplication { get; set; } = string.Empty;
    }
}
