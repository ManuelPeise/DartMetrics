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
    }
}
