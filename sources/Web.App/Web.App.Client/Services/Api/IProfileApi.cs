using global::Shared.Models.User;

namespace Web.App.Client.Services.Api
{
    /// <summary>Profil des angemeldeten Benutzers (ProfileController).</summary>
    public interface IProfileApi
    {
        Task<ApiResult<UserProfileModel>> GetProfileAsync(CancellationToken cancellationToken = default);
    }
}
