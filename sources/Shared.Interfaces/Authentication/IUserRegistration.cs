using Shared.Models.Authentication;
using Shared.Models.Options;
using Shared.Models.User;

namespace Shared.Interfaces.Authentication
{
    public interface IUserRegistration
    {
        Task<bool> RegisterUser(UserRegistrationRequestModel registrationRequestModel, JwtOptions jwtOptions, ServerOptions serverOptions);
        Task<bool> VerifyEmail(string token, JwtOptions jwtOptions, ServerOptions serverOptions);
    }
}
