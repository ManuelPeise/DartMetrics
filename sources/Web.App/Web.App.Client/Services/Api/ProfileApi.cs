using global::Shared.Models.Authentication;
using global::Shared.Models.User;

namespace Web.App.Client.Services.Api
{
    public sealed class ProfileApi : IProfileApi
    {
        private readonly ApiClient _api;

        public ProfileApi(ApiClient api)
        {
            _api = api;
        }

        public Task<ApiResult<UserProfileModel>> GetProfileAsync(CancellationToken cancellationToken = default) =>
            ApiResponse.ReadAsync<UserProfileModel>(_api.GetAsync("Profile/GetProfile", cancellationToken), cancellationToken);

        public Task<ApiResult<UserProfileModel>> UpdateProfileAsync(UserProfileModel model, CancellationToken cancellationToken = default) =>
            ApiResponse.ReadAsync<UserProfileModel>(_api.PostAsJsonAsync("Profile/UpdateProfile", model, cancellationToken), cancellationToken);

        public Task<ApiResult<UploadAvatarResult>> UploadAvatarAsync(UploadAvatarModel model, CancellationToken cancellationToken = default) =>
            ApiResponse.ReadAsync<UploadAvatarResult>(_api.PostAsJsonAsync("Profile/UploadAvatar", model, cancellationToken), cancellationToken);

        public Task<ApiResult<bool>> DeleteAvatarAsync(CancellationToken cancellationToken = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync<object?>("Profile/DeleteAvatar", null, cancellationToken), cancellationToken);

        public Task<ApiResult<bool>> DeleteOwnAccountAsync(DeleteOwnAccountModel model, CancellationToken cancellationToken = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync("Profile/DeleteOwnAccount", model, cancellationToken), cancellationToken);

        public Task<ApiResult<bool>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync("Profile/ChangePassword", request, cancellationToken), cancellationToken);
    }
}
