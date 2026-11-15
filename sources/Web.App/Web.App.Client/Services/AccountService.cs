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

        /// <summary>Bestätigungsmail erneut anfordern (Antwort verrät nicht, ob es das Konto gibt).</summary>
        Task<bool> ResendVerificationAsync(string emailAddress, CancellationToken cancellationToken = default);

        /// <summary>Link zum Zurücksetzen anfordern (Antwort verrät nicht, ob es das Konto gibt).</summary>
        Task<bool> ForgotPasswordAsync(string emailAddress, CancellationToken cancellationToken = default);

        /// <summary>false = Link ungültig, abgelaufen oder schon benutzt.</summary>
        Task<bool> ResetPasswordAsync(string token, string password, string passwordReplication, CancellationToken cancellationToken = default);
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

            ThrowIfRateLimited(response);

            if (!response.IsSuccessStatusCode)
                return false;

            _authenticationStateProvider.Refresh();
            return true;
        }

        public async Task<bool> RegisterAsync(RegisterModel model, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync("bff/register", model, cancellationToken);

            ThrowIfRateLimited(response);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                throw new HttpRequestException("API not reachable.");

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync("bff/verify-email", new VerifyEmailModel { Token = token }, cancellationToken);

            ThrowIfRateLimited(response);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                throw new HttpRequestException("API not reachable.");

            return response.IsSuccessStatusCode;
        }

        public Task<bool> ResendVerificationAsync(string emailAddress, CancellationToken cancellationToken = default) =>
            PostAsync("bff/resend-verification", new { EmailAddress = emailAddress }, cancellationToken);

        public Task<bool> ForgotPasswordAsync(string emailAddress, CancellationToken cancellationToken = default) =>
            PostAsync("bff/forgot-password", new { EmailAddress = emailAddress }, cancellationToken);

        public Task<bool> ResetPasswordAsync(string token, string password, string passwordReplication, CancellationToken cancellationToken = default) =>
            PostAsync("bff/reset-password", new { Token = token, Password = password, PasswordReplication = passwordReplication }, cancellationToken);

        private async Task<bool> PostAsync(string path, object body, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.PostAsJsonAsync(path, body, cancellationToken);

            ThrowIfRateLimited(response);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                throw new HttpRequestException("API not reachable.");

            return response.IsSuccessStatusCode;
        }

        // 429 from the BFF rate limiter: the pages show "too many attempts" instead of "wrong password".
        private static void ThrowIfRateLimited(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("Too many requests.", null, System.Net.HttpStatusCode.TooManyRequests);
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
