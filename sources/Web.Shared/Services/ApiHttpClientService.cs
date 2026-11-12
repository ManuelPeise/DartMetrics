using Microsoft.Extensions.Options;
using Shared.Interfaces.Blazor.Api;
using Shared.Models.Options.Blazor;

namespace Web.Shared.Services
{
    public sealed class ApiHttpClientService : IApiHttpClientService
    {
        private readonly HttpClient _httpClient;

        public ApiHttpClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetAsync(
            string requestUri,
            KeyValuePair<string, string>? authHeader = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                requestUri);

            AddAuthorizationHeader(request, authHeader);

            return await _httpClient.SendAsync(
                request,
                cancellationToken);
        }

        public async Task<HttpResponseMessage> PostAsync(
            string requestUri,
            HttpContent? content = null,
            KeyValuePair<string, string>? authHeader = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                requestUri)
            {
                Content = content
            };

            AddAuthorizationHeader(request, authHeader);

            return await _httpClient.SendAsync(
                request,
                cancellationToken);
        }

        private static void AddAuthorizationHeader(
            HttpRequestMessage request,
            KeyValuePair<string, string>? authHeader)
        {
            if (authHeader.HasValue)
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        authHeader.Value.Key,
                        authHeader.Value.Value);
            }
        }
    }
}
