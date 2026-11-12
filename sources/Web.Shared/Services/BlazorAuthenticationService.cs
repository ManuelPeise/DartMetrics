using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using MySqlX.XDevAPI;
using Shared.Enums.User;
using Shared.Interfaces.Blazor.Api;
using Shared.Interfaces.Blazor.Authentication;
using Shared.Interfaces.Blazor.BlazorSession;
using Shared.Models.Authentication;
using Shared.Models.Blazor;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Web.Shared.Constants;
using Web.Shared.DI;

namespace Web.Shared.Services
{
    public class BlazorAuthenticationService : IBlazorAuthenticationService
    {
        private readonly IApiHttpClientService _apiHttpClientService;
        private readonly ISessionStore _sessionStore;

        public BlazorAuthenticationService(IApiHttpClientService apiHttpClientService, ISessionStore sessionStore)
        {
            _apiHttpClientService = apiHttpClientService;
            _sessionStore = sessionStore;
        }


        public async Task<bool> LoginAsync(
            HttpContext context,
            AuthenticationRequest request,
            CancellationToken cancellationToken = default)
        {
            using var response = await _apiHttpClientService.PostAsync(
                "userauthentication/userlogin",
                JsonContent.Create(request),
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var tokenResponse =
                await response.Content.ReadFromJsonAsync<TokenResponse>(
                    cancellationToken: cancellationToken);

            if (tokenResponse == null ||
                string.IsNullOrWhiteSpace(tokenResponse.Token) ||
                string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
            {
                return false;
            }

            var tokenClaims = GetClaimsFromJwt(tokenResponse.Token);

            var userId = tokenClaims
                .FirstOrDefault(c => c.Type == BlazorClaimTypes.UserId)?.Value;

            var email = tokenClaims
                .FirstOrDefault(c => c.Type == BlazorClaimTypes.UserEmail)?.Value;

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            var roles = new List<UserRoleEnum>();

            foreach (var claim in tokenClaims.Where(
                c => c.Type == BlazorClaimTypes.UserRoles))
            {
                if (!Enum.TryParse<UserRoleEnum>(
                        claim.Value, true, out var role) ||
                    !Enum.IsDefined(role))
                {
                    return false;
                }

                roles.Add(role);
            }

            var userRoles = roles.Distinct().ToArray();

            var session = new BlazorSession
            {
                AccessToken = tokenResponse.Token,
                RefreshToken = tokenResponse.RefreshToken,
                RefreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresAt,
                AccessTokenExpiresAt = tokenResponse.AccessTokenExpiresAt,
                UserId = userId,
                Email = email,
                Roles = userRoles
            };

            var sessionId = await _sessionStore.CreateAsync(
                session,
                cancellationToken);

            var claims = new List<Claim>
            {
                new(BlazorClaimTypes.SessionId, sessionId),
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Email, email)
            };

            claims.AddRange(
                userRoles.Select(role =>
                    new Claim(ClaimTypes.Role, role.ToString())));

            var identity = new ClaimsIdentity(
                claims,
                WebServiceRegistration.Scheme,
                ClaimTypes.Email,
                ClaimTypes.Role);

            var principal = new ClaimsPrincipal(identity);

            try
            {
                await context.SignInAsync(
                    WebServiceRegistration.Scheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        AllowRefresh = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    });
            }
            catch
            {
                await _sessionStore.DeleteAsync(
                    sessionId,
                    cancellationToken);

                throw;
            }

            return true;
        }

        public async Task<TokenResponse?> RefreshAsync(
            string refreshToken,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var content = JsonContent.Create(
                new RefreshTokenRequest
                {
                    RefreshToken = refreshToken
                });

            // Route = api/[controller]/[action] -> action name is "RefreshToken".
            using var response = await _apiHttpClientService.PostAsync(
                "userauthentication/refreshtoken",
                content,
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken);
        }

        public async Task LogoutAsync(HttpContext context)
        {
            var sessionId = context.User.Claims.FirstOrDefault(c => c.Type == BlazorClaimTypes.SessionId)?.Value;

            try
            {
                var session = string.IsNullOrWhiteSpace(sessionId)
                    ? null
                    : await _sessionStore.GetAsync(sessionId);

                if (session != null && !string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    // The API endpoint requires the bearer token to identify the user and revoke the refresh token.
                    using var response = await _apiHttpClientService.PostAsync(
                        "userauthentication/userlogout",
                        null,
                        new KeyValuePair<string, string>("Bearer", session.AccessToken));
                }
            }
            catch (HttpRequestException)
            {
                // The API may be unreachable - the local sign-out must happen anyway.
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    await _sessionStore.DeleteAsync(sessionId);
                }

                await context.SignOutAsync(WebServiceRegistration.Scheme);
            }
        }

        private List<Claim> GetClaimsFromJwt(string jwtToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);
            return token.Claims.ToList();
        }
    }
}

