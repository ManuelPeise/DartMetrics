using Shared.Models.Authentication;
using Shared.Models.Options;
using Shared.Models.User;

namespace Shared.Interfaces.Authentication
{
    public interface IUserRegistration
    {
        Task<bool> RegisterUser(UserRegistrationRequestModel registrationRequestModel, JwtOptions jwtOptions, ServerOptions serverOptions);
        Task<bool> VerifyEmail(string token, JwtOptions jwtOptions, ServerOptions serverOptions);

        /// <summary>
        /// Schickt die Bestätigungsmail erneut, falls es ein unbestätigtes Konto zu der Adresse gibt.
        /// Verrät nicht, ob die Adresse existiert; höchstens eine Mail je Adresse alle 2 Minuten.
        /// </summary>
        Task ResendVerificationMail(string emailAddress, JwtOptions jwtOptions, ServerOptions serverOptions);

        /// <summary>
        /// Schickt einen Link zum Zurücksetzen (60 Minuten gültig, einmal nutzbar), falls es ein aktives, bestätigtes Konto gibt.
        /// Verrät nicht, ob die Adresse existiert; höchstens eine Mail je Adresse alle 2 Minuten.
        /// </summary>
        Task RequestPasswordReset(string emailAddress, JwtOptions jwtOptions, ServerOptions serverOptions);

        /// <summary>Setzt das neue Passwort; false bei ungültigem, abgelaufenem oder bereits benutztem Link. Meldet überall ab.</summary>
        Task<bool> ResetPassword(ResetPasswordRequestModel model, JwtOptions jwtOptions);
    }
}
