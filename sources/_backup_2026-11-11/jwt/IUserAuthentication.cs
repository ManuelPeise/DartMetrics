using Shared.Models.Authentication;

namespace Shared.Interfaces.Authentication
{
    public interface IUserAuthentication
    {
        Task<TokenResponse?> AuthenticateUser(AuthenticationRequest authenticationRequest, string jwtSecurityKey, int jwtTokenExpirationMinutes, int refreshTokenExpirationDays);
    }
}
