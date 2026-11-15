using Data.DartContext.Entities.User;

namespace Shared.Interfaces.User
{
    /// <summary>
    /// Kontolöschung (Admin oder der Benutzer selbst): persönliche Daten ersetzen, Login-Daten und Profilbild löschen,
    /// Bot-Spiele und Turniere anonym behalten (laufende Bot-Spiele enden, offene Turnier-Matches gewinnt der Gegner kampflos).
    /// Rechte prüft der Aufrufer.
    /// </summary>
    public interface IUserAnonymizer
    {
        /// <summary>Erwartet den getrackten Benutzer mit UserCredentials. Speichert selbst.</summary>
        Task AnonymizeAsync(UserEntity user);
    }
}
