using Microsoft.AspNetCore.Http;
using Shared.Models.Authentication;

namespace Shared.Interfaces.Blazor.Authentication
{
    public interface IBlazorAuthenticationService
    {
        Task<bool> LoginAsync(
             HttpContext context,
             AuthenticationRequest request,
             CancellationToken cancellationToken = default);
        Task<TokenResponse?> RefreshAsync(
            string refreshToken,
            string accessToken,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(HttpContext context);
    }
}
