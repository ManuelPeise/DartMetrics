using global::Shared.Models.Authentication;
using global::Shared.Models.User;

namespace Web.App.Client.Services.Api
{
    /// <summary>Profil des angemeldeten Benutzers (ProfileController).</summary>
    public interface IProfileApi
    {
        Task<ApiResult<UserProfileModel>> GetProfileAsync(CancellationToken cancellationToken = default);

        Task<ApiResult<UserProfileModel>> UpdateProfileAsync(UserProfileModel model, CancellationToken cancellationToken = default);

        /// <summary>Liefert die neue Bildversion.</summary>
        Task<ApiResult<UploadAvatarResult>> UploadAvatarAsync(UploadAvatarModel model, CancellationToken cancellationToken = default);

        Task<ApiResult<bool>> DeleteAvatarAsync(CancellationToken cancellationToken = default);

        Task<ApiResult<bool>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

        /// <summary>Eigenes Konto löschen (Bestätigung mit Passwort); danach abmelden.</summary>
        Task<ApiResult<bool>> DeleteOwnAccountAsync(DeleteOwnAccountModel model, CancellationToken cancellationToken = default);
    }
}
