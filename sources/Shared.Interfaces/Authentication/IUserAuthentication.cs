using Shared.Models.Authentication;

namespace Shared.Interfaces.Authentication
{
    public interface IUserAuthentication
    {
        Task<TokenResponse?> AuthenticateUser(AuthenticationRequest authenticationRequest, JwtOptions jwtOptions);
        Task<TokenResponse?> RefreshToken(RefreshTokenRequest refreshTokenRequest, JwtOptions jwtOptions);
        Task<bool> LogoutUser();
    }
}
