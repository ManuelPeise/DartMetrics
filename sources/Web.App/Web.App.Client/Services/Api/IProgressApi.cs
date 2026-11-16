using global::Shared.Models.Progression;

namespace Web.App.Client.Services.Api
{
    /// <summary>Level, Erfolge und Rekorde (PlayerProgressController).</summary>
    public interface IProgressApi
    {
        Task<ApiResult<PlayerProgress>> GetMyProgressAsync(CancellationToken cancellationToken = default);
    }
}
