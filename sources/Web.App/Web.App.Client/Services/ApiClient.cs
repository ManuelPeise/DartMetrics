using System.Net;
using System.Net.Http.Json;
using Web.App.Client.Authentication;

namespace Web.App.Client.Services
{
    /// <summary>
    /// Calls the DartMetrics API from WebAssembly via the server proxy (/bff/api/...).
    /// The server attaches the access token and refreshes it if necessary.
    /// Example: Api.GetFromJsonAsync&lt;ProfileModel&gt;("profile/getprofile")
    /// </summary>
    public sealed class ApiClient
    {
        private const string Prefix = "bff/api/";

        private readonly HttpClient _httpClient;
        private readonly BffAuthenticationStateProvider _authenticationStateProvider;

        public ApiClient(IHttpClientFactory httpClientFactory, BffAuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClientFactory.CreateClient(BffHttpClient.Name);
            _authenticationStateProvider = authenticationStateProvider;
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default) =>
            HandleAsync(_httpClient.GetAsync(Prefix + requestUri.TrimStart('/'), cancellationToken));

        public Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T body, CancellationToken cancellationToken = default) =>
            HandleAsync(_httpClient.PostAsJsonAsync(Prefix + requestUri.TrimStart('/'), body, cancellationToken));

        public Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T body, CancellationToken cancellationToken = default) =>
            HandleAsync(_httpClient.PutAsJsonAsync(Prefix + requestUri.TrimStart('/'), body, cancellationToken));

        public Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default) =>
            HandleAsync(_httpClient.DeleteAsync(Prefix + requestUri.TrimStart('/'), cancellationToken));

        public async Task<T?> GetFromJsonAsync<T>(string requestUri, CancellationToken cancellationToken = default)
        {
            using var response = await GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }

        private async Task<HttpResponseMessage> HandleAsync(Task<HttpResponseMessage> request)
        {
            var response = await request;

            // Session ended on the server (refresh token expired/revoked) -> update UI (AuthorizeView etc.).
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                _authenticationStateProvider.Refresh();

            return response;
        }
    }
}
