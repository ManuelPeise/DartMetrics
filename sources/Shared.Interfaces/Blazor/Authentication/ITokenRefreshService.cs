namespace Shared.Interfaces.Blazor.Authentication
{
    public interface ITokenRefreshService
    {
        Task<bool> RefreshIfNeededAsync(
            string sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>Refreshes regardless of the stored expiry (e.g. after the API answered 401).</summary>
        Task<bool> ForceRefreshAsync(
            string sessionId,
            CancellationToken cancellationToken = default);
    }
}
