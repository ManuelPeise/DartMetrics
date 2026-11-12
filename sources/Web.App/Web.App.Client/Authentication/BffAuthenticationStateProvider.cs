using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
using Web.App.Client.Services;

namespace Web.App.Client.Authentication
{
    /// <summary>
    /// AuthenticationStateProvider for the WebAssembly part. Asks the server (GET /bff/user) who is signed in.
    /// The auth cookie is HttpOnly, so the client never sees it or any JWT.
    /// </summary>
    public sealed class BffAuthenticationStateProvider : AuthenticationStateProvider
    {
        public const string AuthenticationType = "DartMetrics.Bff";

        private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

        private readonly HttpClient _httpClient;
        private Task<AuthenticationState>? _authenticationState;

        public BffAuthenticationStateProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(BffHttpClient.Name);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            _authenticationState ??= LoadAsync();

        /// <summary>Reloads the user from the server and notifies all AuthorizeViews (after login/logout/401).</summary>
        public void Refresh()
        {
            _authenticationState = LoadAsync();
            NotifyAuthenticationStateChanged(_authenticationState);
        }

        private async Task<AuthenticationState> LoadAsync()
        {
            try
            {
                var user = await _httpClient.GetFromJsonAsync<BffUserInfo>("bff/user");

                if (user is not { IsAuthenticated: true } || string.IsNullOrWhiteSpace(user.Name))
                    return Anonymous;

                var claims = new List<Claim> { new(ClaimTypes.Name, user.Name) };

                if (!string.IsNullOrWhiteSpace(user.UserId))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserId));

                claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var identity = new ClaimsIdentity(claims, AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);

                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch (HttpRequestException)
            {
                return Anonymous;
            }
        }
    }
}
