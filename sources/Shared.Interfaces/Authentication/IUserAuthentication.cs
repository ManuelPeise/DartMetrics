using Shared.Models.Authentication;

namespace Shared.Interfaces.Authentication
{
    public interface IUserAuthentication
    {
        Task<TokenResponse?> AuthenticateUser(AuthenticationRequest authenticationRequest, JwtOptions jwtOptions);
        Task<bool> LogoutUser();
    }
}
