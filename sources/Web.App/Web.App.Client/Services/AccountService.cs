using System.Net.Http.Json;
using Web.App.Client.Authentication;

namespace Web.App.Client.Services
{
    public interface IAccountService
    {
        Task<bool> LoginAsync(LoginModel model, CancellationToken cancellationToken = default);
        Task LogoutAsync(CancellationToken cancellationToken = default);
        Task<bool> RegisterAsync(RegisterModel model, CancellationToken cancellationToken = default);
        Task<bool> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);
    }

    public sealed class AccountService : IAccountService
    {
        private readonly HttpClient _httpClient;
        private readonly BffAuthenticationStateProvider _authenticationStateProvider;

        public AccountService(IHttpClientFactory httpClientFactory, BffAuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClientFactory.CreateClient(BffHttpClient.Name);
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<bool> LoginAsync(LoginModel model, CancellationToken cancellationToken = default)
        {
            // The server calls UserAuthenticationController/UserLogin and sets the HttpOnly auth cookie.
            using var response = await _httpClient.PostAsJsonAsync("bff/login", model, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            _authenticationStateProvider.Refresh();
            return true;
        }

        public async Task<bool> RegisterAsync(RegisterModel model, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync("bff/register", model, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                throw new HttpRequestException("API not reachable.");

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync("bff/verify-email", new VerifyEmailModel { Token = token }, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                throw new HttpRequestException("API not reachable.");

            return response.IsSuccessStatusCode;
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.PostAsync("bff/logout", null, cancellationToken);
            }
            finally
            {
                _authenticationStateProvider.Refresh();
            }
        }
    }
}
