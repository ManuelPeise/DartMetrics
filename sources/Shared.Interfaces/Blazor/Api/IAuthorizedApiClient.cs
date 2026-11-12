namespace Shared.Interfaces.Blazor.Api
{
    /// <summary>
    /// API client for calls on behalf of the signed-in user. Attaches the access token of the current
    /// Blazor session, refreshes it shortly before it expires and retries once after a 401.
    /// </summary>
    public interface IAuthorizedApiClient
    {
        Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> PostAsync(string requestUri, object? body = null, CancellationToken cancellationToken = default);

        Task<T?> GetFromJsonAsync<T>(string requestUri, CancellationToken cancellationToken = default);

        Task<TResponse?> PostAsJsonAsync<TResponse>(string requestUri, object? body = null, CancellationToken cancellationToken = default);
    }
}
