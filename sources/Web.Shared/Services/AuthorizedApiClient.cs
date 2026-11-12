using Microsoft.AspNetCore.Components.Authorization;
using Shared.Interfaces.Blazor.Api;
using Shared.Interfaces.Blazor.Authentication;
using Shared.Interfaces.Blazor.BlazorSession;
using System.Net;
using System.Net.Http.Json;
using Web.Shared.Constants;

namespace Web.Shared.Services
{
    /// <summary>
    /// Scoped (= one instance per request / per Blazor circuit), therefore it sees the correct user.
    /// The session id comes from the AuthenticationState, which also works inside an interactive
    /// server circuit where no HttpContext is available.
    /// </summary>
    public sealed class AuthorizedApiClient : IAuthorizedApiClient
    {
        private readonly IApiHttpClientService _apiHttpClientService;
        private readonly ITokenRefreshService _tokenRefreshService;
        private readonly ISessionStore _sessionStore;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public AuthorizedApiClient(
            IApiHttpClientService apiHttpClientService,
            ITokenRefreshService tokenRefreshService,
            ISessionStore sessionStore,
            AuthenticationStateProvider authenticationStateProvider)
        {
            _apiHttpClientService = apiHttpClientService;
            _tokenRefreshService = tokenRefreshService;
            _sessionStore = sessionStore;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default) =>
            SendAsync(
                (authHeader, ct) => _apiHttpClientService.GetAsync(requestUri, authHeader, ct),
                cancellationToken);

        public Task<HttpResponseMessage> PostAsync(string requestUri, object? body = null, CancellationToken cancellationToken = default) =>
            // The content is created per attempt, because HttpContent cannot be sent twice.
            SendAsync(
                (authHeader, ct) => _apiHttpClientService.PostAsync(
                    requestUri,
                    body == null ? null : JsonContent.Create(body),
                    authHeader,
                    ct),
                cancellationToken);

        public async Task<T?> GetFromJsonAsync<T>(string requestUri, CancellationToken cancellationToken = default)
        {
            using var response = await GetAsync(requestUri, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }

        public async Task<TResponse?> PostAsJsonAsync<TResponse>(string requestUri, object? body = null, CancellationToken cancellationToken = default)
        {
            using var response = await PostAsync(requestUri, body, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsync(
            Func<KeyValuePair<string, string>?, CancellationToken, Task<HttpResponseMessage>> send,
            CancellationToken cancellationToken)
        {
            var sessionId = await GetSessionIdAsync();

            if (sessionId == null)
                return Unauthorized();

            // 1) Proactive: refresh if the access token expires within the next 2 minutes.
            if (!await _tokenRefreshService.RefreshIfNeededAsync(sessionId, cancellationToken))
                return Unauthorized();

            var response = await send(await GetAuthHeaderAsync(sessionId, cancellationToken), cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            // 2) Reactive: token was rejected anyway (e.g. clock skew, server restart) -> force refresh, retry once.
            response.Dispose();

            if (!await _tokenRefreshService.ForceRefreshAsync(sessionId, cancellationToken))
                return Unauthorized();

            return await send(await GetAuthHeaderAsync(sessionId, cancellationToken), cancellationToken);
        }

        private async Task<string?> GetSessionIdAsync()
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();

            var sessionId = state.User.FindFirst(BlazorClaimTypes.SessionId)?.Value;

            return string.IsNullOrWhiteSpace(sessionId) ? null : sessionId;
        }

        private async Task<KeyValuePair<string, string>?> GetAuthHeaderAsync(string sessionId, CancellationToken cancellationToken)
        {
            var session = await _sessionStore.GetAsync(sessionId, cancellationToken);

            return string.IsNullOrWhiteSpace(session?.AccessToken)
                ? null
                : new KeyValuePair<string, string>("Bearer", session.AccessToken);
        }

        private static HttpResponseMessage Unauthorized() => new(HttpStatusCode.Unauthorized);
    }
}
