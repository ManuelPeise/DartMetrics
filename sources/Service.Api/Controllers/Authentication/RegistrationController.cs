using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Interfaces.Authentication;
using Shared.Models.Authentication;
using Shared.Models.Options;

namespace Service.Api.Controllers.Authentication
{
    public class RegistrationController : ApiControllerBase
    {
        private readonly IUserRegistration _userRegistration;
        private readonly ServerOptions _serverOptions;
        private readonly JwtOptions _jwtOptions;

        public RegistrationController(IUserRegistration userRegistration, IOptions<JwtOptions> jwtOptions, IOptions<ServerOptions> serverOptions)
        {
            _userRegistration = userRegistration;
            _jwtOptions = jwtOptions.Value;
            _serverOptions = serverOptions.Value;
        }

        [AllowAnonymous]
        [HttpPost(Name = "RegisterUser")]
        public async Task<bool> RegisterUser([FromBody] Shared.Models.User.UserRegistrationRequestModel registrationRequestModel)
        {
            return await _userRegistration.RegisterUser(registrationRequestModel, _jwtOptions, _serverOptions);
        }

        [AllowAnonymous]
        [HttpPost(Name = "VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var verified = await _userRegistration.VerifyEmail(token, _jwtOptions, _serverOptions);

            return verified ? Ok() : BadRequest();
        }
    }
}
