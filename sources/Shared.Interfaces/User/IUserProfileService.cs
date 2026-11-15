using Shared.Models.Authentication;
using Shared.Models.User;

namespace Shared.Interfaces.User
{
    public interface IUserProfileService
    {
        Task<UserProfileModel> GetProfileData();
        Task<UserProfileModel> UpdateUserProfile(UserProfileModel model);
        Task ChangePassword(ChangePasswordRequest request);

        /// <summary>
        /// Eigenes Konto löschen (anonymisieren), bestätigt mit dem aktuellen Passwort. Nicht für Admins
        /// (damit niemand die Verwaltung versehentlich verliert). Danach ist keine Anmeldung mehr möglich.
        /// </summary>
        Task DeleteOwnAccount(DeleteOwnAccountModel model);

        /// <summary>Speichert das Profilbild (ersetzt ein vorhandenes) und liefert die neue Version.</summary>
        Task<string> UploadAvatar(UploadAvatarModel model);

        Task DeleteAvatar();

        /// <summary>Eigenes Profilbild, null wenn keins vorhanden.</summary>
        Task<AvatarImage?> GetAvatar();
    }
}
