using global::Shared.Models.Progression;

namespace Web.App.Client.Services.Api
{
    public sealed class ProgressApi : IProgressApi
    {
        private readonly ApiClient _api;

        public ProgressApi(ApiClient api)
        {
            _api = api;
        }

        public Task<ApiResult<PlayerProgress>> GetMyProgressAsync(CancellationToken cancellationToken = default)
            => ApiResponse.ReadAsync<PlayerProgress>(_api.GetAsync("PlayerProgress/GetMyProgress", cancellationToken), cancellationToken);
    }
}
