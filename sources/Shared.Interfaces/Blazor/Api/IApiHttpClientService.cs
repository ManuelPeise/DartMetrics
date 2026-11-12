

namespace Shared.Interfaces.Blazor.Api
{
    public interface IApiHttpClientService
    {
        Task<HttpResponseMessage> GetAsync(string requestUri, KeyValuePair<string, string>? AuthHeader = null, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content = null, KeyValuePair<string, string>? AuthHeader = null, CancellationToken cancellationToken = default);
    }
}
